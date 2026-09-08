using Microsoft.Data.SqlClient;

namespace KindleClippings.ConsoleApp.Ai;

public sealed class ClippingDiscoveryRepository(string connectionString)
{
    public async Task<List<AiClipping>> LoadAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id, Title, Author, Text
            FROM dbo.vw_ExportableClipping
            WHERE Type = 'Highlight'
              AND NULLIF(LTRIM(RTRIM(Text)), '') IS NOT NULL;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new List<AiClipping>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var text = reader.GetString(reader.GetOrdinal("Text"));
            result.Add(new AiClipping(
                reader.GetString(reader.GetOrdinal("Id")),
                reader.GetString(reader.GetOrdinal("Title")),
                GetNullableString(reader, "Author"),
                text,
                LanguageDetector.Detect(text)));
        }

        return result;
    }

    private static string? GetNullableString(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
