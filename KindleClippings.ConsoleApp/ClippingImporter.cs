using System.Data;
using Microsoft.Data.SqlClient;

namespace KindleClippings.ConsoleApp;

public static class ClippingImporter
{
    private const string ConnectionString =
        "Server=localhost\\SQLDEV;" +
        "Database=KindleClippings;" +
        "Trusted_Connection=True;" +
        "TrustServerCertificate=True;";

    public static async Task<(int Inserted, int Updated)> ImportAsync(
        IEnumerable<Clipping> clippings,
        CancellationToken cancellationToken = default)
    {
        var table = CreateDataTable();

        foreach (var clipping in clippings)
        {
            var row = table.NewRow();

            row["Id"] = clipping.Id;
            row["Title"] = clipping.Book.Title;
            row["Author"] = clipping.Book.Author ?? (object)DBNull.Value;
            row["Type"] = clipping.Type.ToString();
            row["Page"] = clipping.Page ?? (object)DBNull.Value;
            row["StartLocation"] = clipping.StartLocation ?? (object)DBNull.Value;
            row["EndLocation"] = clipping.EndLocation ?? (object)DBNull.Value;
            row["AddedOn"] = clipping.AddedOn ?? (object)DBNull.Value;
            row["Text"] = clipping.Text ?? (object)DBNull.Value;
            row["ImportedAt"] = DateTime.Now;

            table.Rows.Add(row);
        }

        if (table.Rows.Count == 0)
            return (0, 0);

        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = (SqlTransaction)
            await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await CreateTempTableAsync(
                connection,
                transaction,
                cancellationToken);

            await BulkCopyAsync(
                connection,
                transaction,
                table,
                cancellationToken);

            var result = await MergeAsync(
                connection,
                transaction,
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task CreateTempTableAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TOP (0) *
            INTO #Clipping
            FROM dbo.Clipping;
            """;

        await using var command = new SqlCommand(
            sql,
            connection,
            transaction);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task BulkCopyAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        DataTable table,
        CancellationToken cancellationToken)
    {
        using var bulkCopy = new SqlBulkCopy(
            connection,
            SqlBulkCopyOptions.Default,
            transaction);

        bulkCopy.DestinationTableName = "#Clipping";
        bulkCopy.BatchSize = 1000;
        bulkCopy.BulkCopyTimeout = 0;

        foreach (DataColumn column in table.Columns)
        {
            bulkCopy.ColumnMappings.Add(
                column.ColumnName,
                column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(
            table,
            cancellationToken);
    }

    private static async Task<(int Inserted, int Updated)> MergeAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        const string sql = """
            MERGE dbo.Clipping AS target
            USING #Clipping AS source
                ON target.Id = source.Id

            WHEN MATCHED THEN
                UPDATE SET
                    Title         = source.Title,
                    Author        = source.Author,
                    Type          = source.Type,
                    Page          = source.Page,
                    StartLocation = source.StartLocation,
                    EndLocation   = source.EndLocation,
                    AddedOn       = source.AddedOn,
                    Text          = source.Text

            WHEN NOT MATCHED BY TARGET THEN
                INSERT
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
                    source.Id,
                    source.Title,
                    source.Author,
                    source.Type,
                    source.Page,
                    source.StartLocation,
                    source.EndLocation,
                    source.AddedOn,
                    source.Text
                )

            OUTPUT $action;
            """;

        int inserted = 0;
        int updated = 0;

        await using var command = new SqlCommand(
            sql,
            connection,
            transaction);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            switch (reader.GetString(0))
            {
                case "INSERT":
                    inserted++;
                    break;

                case "UPDATE":
                    updated++;
                    break;
            }
        }

        return (inserted, updated);
    }

    private static DataTable CreateDataTable()
    {
        var table = new DataTable();

        table.Columns.Add("Id", typeof(string));
        table.Columns.Add("Title", typeof(string));
        table.Columns.Add("Author", typeof(string));
        table.Columns.Add("Type", typeof(string));
        table.Columns.Add("Page", typeof(int));
        table.Columns.Add("StartLocation", typeof(int));
        table.Columns.Add("EndLocation", typeof(int));
        table.Columns.Add("AddedOn", typeof(DateTime));
        table.Columns.Add("Text", typeof(string));
        table.Columns.Add("ImportedAt", typeof(DateTime));

        return table;
    }

    public static async Task<int> AssignIdsToOldClippingsAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
            const string selectSql = """
            SELECT
                RowId,
                Title,
                Author,
                Type,
                Page,
                StartLocation,
                EndLocation,
                AddedOn,
                Text,
                ImportedAt
            FROM dbo.OldClipping
            WHERE Id IS NULL OR Id = ''
            ORDER BY RowId
            """;

            await using var selectCommand = new SqlCommand(
                selectSql, connection, (SqlTransaction)transaction);

            await using var reader = await selectCommand.ExecuteReaderAsync();

            var clippings = new List<(int RowId, Clipping Clipping)>();

            while (await reader.ReadAsync())
            {
                var clipping = new Clipping
                {
                    Book = new Book
                    {
                        Title = reader.GetString(reader.GetOrdinal("Title")),
                        Author = reader.IsDBNull(reader.GetOrdinal("Author"))
                            ? null
                            : reader.GetString(reader.GetOrdinal("Author"))
                    },

                    Type = Enum.Parse<ClippingType>(
                        reader.GetString(reader.GetOrdinal("Type")),
                        ignoreCase: true),

                    Page = reader.IsDBNull(reader.GetOrdinal("Page"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("Page")),

                    StartLocation = reader.IsDBNull(reader.GetOrdinal("StartLocation"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("StartLocation")),

                    EndLocation = reader.IsDBNull(reader.GetOrdinal("EndLocation"))
                        ? null
                        : reader.GetInt32(reader.GetOrdinal("EndLocation")),

                    AddedOn = reader.IsDBNull(reader.GetOrdinal("AddedOn"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("AddedOn")),

                    Text = reader.IsDBNull(reader.GetOrdinal("Text"))
                        ? null
                        : reader.GetString(reader.GetOrdinal("Text")),

                    ImportedAt = reader.IsDBNull(reader.GetOrdinal("ImportedAt"))
                        ? null
                        : reader.GetDateTime(reader.GetOrdinal("ImportedAt"))
                };

                clipping.Id = ClippingIdGenerator.CreateId(clipping);

                var rowId = reader.GetInt32(reader.GetOrdinal("RowId"));

                clippings.Add((rowId, clipping));
            }

            await reader.CloseAsync();

            const string updateSql = """
                UPDATE dbo.OldClipping
                SET Id = @Id
                WHERE RowId = @RowId
                """;

            await using var updateCommand = new SqlCommand(
                updateSql, connection, (SqlTransaction)transaction);

            updateCommand.Parameters.Add("@Id", SqlDbType.NVarChar, 14);
            updateCommand.Parameters.Add("@RowId", SqlDbType.Int);

            foreach (var (rowId, clipping) in clippings)
            {
                updateCommand.Parameters["@Id"].Value = clipping.Id;
                updateCommand.Parameters["@RowId"].Value = rowId;

                await updateCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            return clippings.Count;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}