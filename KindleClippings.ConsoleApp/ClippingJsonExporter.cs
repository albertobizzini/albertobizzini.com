using Microsoft.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace KindleClippings.Console;

public static class ClippingJsonExporter
{
    private const string ConnectionString =
        "Server=localhost\\SQLDEV;" +
        "Database=KindleClippings;" +
        "Trusted_Connection=True;" +
        "TrustServerCertificate=True;";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static async Task<int> ExportAsync(
        string jsonFile,
        CancellationToken cancellationToken = default,
        [CallerFilePath] string sourceFilePath = "")
    {
        const string sql = """
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

        await using var connection =
            new SqlConnection(ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(sql, connection);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        var clippings = new List<Clipping>();

        while (await reader.ReadAsync(cancellationToken))
        {
            clippings.Add(ReadClipping(reader));
        }

        // 1. Se il percorso è relativo, lo ancora alla cartella del codice sorgente (visto che solitamente questo metodo sarà chiamato periodicamente solo da ambiente di sviluppo)
        if (!Path.IsPathRooted(jsonFile))
        {
            var sourceDirectory = Path.GetDirectoryName(sourceFilePath);
            jsonFile = Path.GetFullPath(Path.Combine(sourceDirectory, jsonFile));
        }

        // 2. Crea la cartella di destinazione (es. wwwroot/data/) se manca
        var directory = Path.GetDirectoryName(jsonFile);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        if (File.Exists(jsonFile))
            File.Delete(jsonFile);

        await using var stream = new FileStream(
            jsonFile,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        await JsonSerializer.SerializeAsync(
            stream,
            clippings,
            JsonOptions,
            cancellationToken);

        return clippings.Count;
    }

    private static Clipping ReadClipping(
        SqlDataReader reader)
    {
        var title = reader.GetString(
            reader.GetOrdinal("Title"));

        var authorOrdinal =
            reader.GetOrdinal("Author");

        var author =
            reader.IsDBNull(authorOrdinal)
                ? null
                : reader.GetString(authorOrdinal);

        var type = Enum.Parse<ClippingType>(
            reader.GetString(
                reader.GetOrdinal("Type")));

        return new Clipping
        {
            Id = reader.GetString(
                reader.GetOrdinal("Id")),

            Book = new Book
            {
                Title = title,
                Author = author
            },

            Type = type,

            Page = GetNullableInt32(
                reader,
                "Page"),

            StartLocation = GetNullableInt32(
                reader,
                "StartLocation"),

            EndLocation = GetNullableInt32(
                reader,
                "EndLocation"),

            AddedOn = GetNullableDateTime(
                reader,
                "AddedOn"),

            Text = GetNullableString(
                reader,
                "Text")
        };
    }

    private static int? GetNullableInt32(
        SqlDataReader reader,
        string column)
    {
        var ordinal = reader.GetOrdinal(column);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetInt32(ordinal);
    }

    private static DateTime? GetNullableDateTime(
        SqlDataReader reader,
        string column)
    {
        var ordinal = reader.GetOrdinal(column);

        if (reader.IsDBNull(ordinal))
            return null;

        return reader.GetDateTime(ordinal);
    }

    private static string? GetNullableString(
        SqlDataReader reader,
        string column)
    {
        var ordinal = reader.GetOrdinal(column);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetString(ordinal);
    }
}