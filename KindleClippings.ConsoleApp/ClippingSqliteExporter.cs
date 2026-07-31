using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Runtime.CompilerServices;

namespace KindleClippings.ConsoleApp;

public static class ClippingSqliteExporter
{
    private const string SqlServerConnectionString =
        "Server=localhost\\SQLDEV;" +
        "Database=KindleClippings;" +
        "Trusted_Connection=True;" +
        "TrustServerCertificate=True;";

    public static async Task<int> ExportAsync(
        string sqliteFile,
        CancellationToken cancellationToken = default,
        [CallerFilePath] string sourceFilePath = "")
    {
        // 1. Se il percorso è relativo, lo ancora alla cartella del codice sorgente (visto che solitamente questo metodo sarà chiamato periodicamente solo da ambiente di sviluppo)
        if (!Path.IsPathRooted(sqliteFile))
        {
            var sourceDirectory = Path.GetDirectoryName(sourceFilePath);
            sqliteFile = Path.GetFullPath(Path.Combine(sourceDirectory, sqliteFile));
        }

        // 2. Crea la cartella di destinazione (es. wwwroot/data/) se manca
        var directory = Path.GetDirectoryName(sqliteFile);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        if (File.Exists(sqliteFile))
            File.Delete(sqliteFile);

        await using var sqlConnection =
            new SqlConnection(SqlServerConnectionString);

        await sqlConnection.OpenAsync(cancellationToken);

        await using var sqliteConnection =
            new SqliteConnection($"Data Source={sqliteFile}");

        await sqliteConnection.OpenAsync(cancellationToken);

        await using var transaction = (SqliteTransaction)
            await sqliteConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            await CreateSqliteSchemaAsync(
                sqlConnection,
                sqliteConnection,
                transaction,
                cancellationToken);

            var count = await CopyDataAsync(
                sqlConnection,
                sqliteConnection,
                transaction,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return count;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);

            if (File.Exists(sqliteFile))
                File.Delete(sqliteFile);

            throw;
        }
    }

    private static async Task CreateSqliteSchemaAsync(
        SqlConnection sqlConnection,
        SqliteConnection sqliteConnection,
        SqliteTransaction sqliteTransaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                c.name AS ColumnName,
                t.name AS SqlType,
                c.max_length AS MaxLength,
                c.is_nullable AS IsNullable,
                c.column_id AS ColumnId
            FROM sys.columns c
            INNER JOIN sys.types t
                ON c.user_type_id = t.user_type_id
            WHERE c.object_id = OBJECT_ID('dbo.Clipping')
            ORDER BY c.column_id;
            """;

        await using var command =
            new SqlCommand(sql, sqlConnection);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var columns = new List<SqliteColumn>();

        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new SqliteColumn(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt16(2),
                reader.GetBoolean(3),
                reader.GetInt32(4)));
        }

        if (columns.Count == 0)
            throw new InvalidOperationException(
                "La tabella dbo.Clipping non esiste o non contiene colonne.");

        var definitions = columns
            .Select(CreateSqliteColumnDefinition);

        var createSql = $"""
            CREATE TABLE Clipping
            (
                {string.Join(",\n    ", definitions)}
            );
            """;

        await using var createCommand =
            new SqliteCommand(
                createSql,
                sqliteConnection,
                sqliteTransaction);

        await createCommand.ExecuteNonQueryAsync(cancellationToken);

        // Indice sulla chiave primaria
        await using var indexCommand = new SqliteCommand(
            """
            CREATE UNIQUE INDEX IX_Clipping_Id
            ON Clipping(Id);
            """,
            sqliteConnection,
            sqliteTransaction);

        await indexCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string CreateSqliteColumnDefinition(
        SqliteColumn column)
    {
        var type = column.SqlType.ToLowerInvariant() switch
        {
            "varchar" => "TEXT",
            "nvarchar" => "TEXT",
            "char" => "TEXT",
            "nchar" => "TEXT",
            "text" => "TEXT",

            "int" => "INTEGER",
            "bigint" => "INTEGER",
            "smallint" => "INTEGER",
            "tinyint" => "INTEGER",

            "datetime" => "TEXT",
            "datetime2" => "TEXT",
            "date" => "TEXT",

            "bit" => "INTEGER",

            "decimal" => "REAL",
            "numeric" => "REAL",
            "float" => "REAL",
            "real" => "REAL",

            _ => throw new NotSupportedException(
                $"Tipo SQL Server non supportato: {column.SqlType}")
        };

        var primaryKey =
            column.ColumnName.Equals("Id", StringComparison.OrdinalIgnoreCase)
                ? " PRIMARY KEY"
                : "";

        var notNull =
            !column.IsNullable && primaryKey.Length == 0
                ? " NOT NULL"
                : "";

        return $"{QuoteIdentifier(column.ColumnName)} {type}{primaryKey}{notNull}";
    }

    private static async Task<int> CopyDataAsync(
        SqlConnection sqlConnection,
        SqliteConnection sqliteConnection,
        SqliteTransaction sqliteTransaction,
        CancellationToken cancellationToken)
    {
        const string selectSql = """
            SELECT
                Id,
                Title,
                Author,
                Type,
                Page,
                StartLocation,
                EndLocation,
                AddedOn,
                Text
            FROM dbo.vw_ExportableClipping;
            """;

        await using var selectCommand =
            new SqlCommand(selectSql, sqlConnection);

        await using var reader =
            await selectCommand.ExecuteReaderAsync(cancellationToken);

        const string insertSql = """
            INSERT INTO Clipping
            (
                Id,
                Title,
                Author,
                Type,
                Page,
                StartLocation,
                EndLocation,
                AddedOn,
                Text
            )
            VALUES
            (
                $Id,
                $Title,
                $Author,
                $Type,
                $Page,
                $StartLocation,
                $EndLocation,
                $AddedOn,
                $Text
            );
            """;

        await using var insertCommand =
            new SqliteCommand(
                insertSql,
                sqliteConnection,
                sqliteTransaction);

        insertCommand.Parameters.Add("$Id", SqliteType.Text);
        insertCommand.Parameters.Add("$Title", SqliteType.Text);
        insertCommand.Parameters.Add("$Author", SqliteType.Text);
        insertCommand.Parameters.Add("$Type", SqliteType.Text);
        insertCommand.Parameters.Add("$Page", SqliteType.Integer);
        insertCommand.Parameters.Add("$StartLocation", SqliteType.Integer);
        insertCommand.Parameters.Add("$EndLocation", SqliteType.Integer);
        insertCommand.Parameters.Add("$AddedOn", SqliteType.Text);
        insertCommand.Parameters.Add("$Text", SqliteType.Text);

        var count = 0;

        while (await reader.ReadAsync(cancellationToken))
        {
            insertCommand.Parameters["$Id"].Value =
                reader["Id"];

            insertCommand.Parameters["$Title"].Value =
                reader["Title"];

            insertCommand.Parameters["$Author"].Value =
                reader["Author"];

            insertCommand.Parameters["$Type"].Value =
                reader["Type"];

            insertCommand.Parameters["$Page"].Value =
                reader["Page"];

            insertCommand.Parameters["$StartLocation"].Value =
                reader["StartLocation"];

            insertCommand.Parameters["$EndLocation"].Value =
                reader["EndLocation"];

            insertCommand.Parameters["$AddedOn"].Value =
                reader["AddedOn"] is DBNull
                    ? DBNull.Value
                    : ((DateTime)reader["AddedOn"])
                        .ToString("O");

            insertCommand.Parameters["$Text"].Value =
                reader["Text"];

            await insertCommand.ExecuteNonQueryAsync(
                cancellationToken);

            count++;
        }

        return count;
    }

    private static string QuoteIdentifier(string name)
        => "\"" + name.Replace("\"", "\"\"") + "\"";

    private sealed record SqliteColumn(
        string ColumnName,
        string SqlType,
        short MaxLength,
        bool IsNullable,
        int ColumnId);
}