using KindleClippings;
using SqliteWasmBlazor;
using System.Security.Cryptography;
using System.Text;

namespace AlbertoBizzini.Web.Services;

public class KindleClippingService
{
    private SqliteWasmConnection _connection;
    private readonly ILogger<KindleClippingService> _logger;


    public KindleClippingService(
        ILogger<KindleClippingService> logger)
    {
        _logger = logger;
        _connection = new SqliteWasmConnection(
            "Data Source=clippings.db");
    }

    private async Task<SqliteWasmConnection> GetConnectionAsync()
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync();

        return _connection;
    }

    public async Task<List<Clipping>> GetAllClippings()
    {
        var connection = await GetConnectionAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = """
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
            FROM Clipping;
            """;

        await using var reader = await command.ExecuteReaderAsync();

        var clippings = new List<Clipping>();

        while (await reader.ReadAsync())
        {
            var clipping = ReadClipping(reader);
            if (clipping != null)
            {
                clippings.Add(clipping);
            }
        }

        return clippings;
    }


    public async Task<Clipping?> GetClippingById(string id)
    {
        var connection = await GetConnectionAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = """
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
            FROM Clipping
            WHERE Id = $id;
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "$id";
        parameter.Value = id;
        command.Parameters.Add(parameter);

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return ReadClipping(reader);
    }

    public async Task<Clipping?> GetClippingOfTheDayAsync(
        int delta = 0)
    {
        var connection = await GetConnectionAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = """
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
            FROM Clipping
            WHERE Type = 'Highlight'
              AND Text IS NOT NULL
              AND length(trim(Text)) > 0;
            """;

        await using var reader =
            await command.ExecuteReaderAsync();

        var targetDate = DateTime.Today.AddDays(delta);
        var dateString = targetDate.ToString("yyyy-MM-dd");

        Clipping? bestClipping = null;
        long highestScore = long.MinValue;

        while (await reader.ReadAsync())
        {
            var clipping = ReadClipping(reader);

            var clippingId =
                $"{clipping.Book.Title}_{clipping.StartLocation}_{clipping.Text}";

            var dayClippingKey =
                $"{dateString}_{clippingId}";

            var score = GetStableHash64(dayClippingKey);

            if (score > highestScore)
            {
                highestScore = score;
                bestClipping = clipping;
            }
        }

        return bestClipping;
    }

    private static Clipping ReadClipping(
        System.Data.Common.DbDataReader reader)
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

        var page = GetNullableInt32(
            reader,
            "Page");

        var startLocation = GetNullableInt32(
            reader,
            "StartLocation");

        var endLocation = GetNullableInt32(
            reader,
            "EndLocation");

        var addedOn = GetNullableDateTime(
            reader,
            "AddedOn");

        var textOrdinal =
            reader.GetOrdinal("Text");

        var text =
            reader.IsDBNull(textOrdinal)
                ? null
                : reader.GetString(textOrdinal);

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
            Page = page,
            StartLocation = startLocation,
            EndLocation = endLocation,
            AddedOn = addedOn,
            Text = text
        };
    }

    private static int? GetNullableInt32(
        System.Data.Common.DbDataReader reader,
        string column)
    {
        var ordinal = reader.GetOrdinal(column);

        return reader.IsDBNull(ordinal)
            ? null
            : reader.GetInt32(ordinal);
    }

    private static DateTime? GetNullableDateTime(
        System.Data.Common.DbDataReader reader,
        string column)
    {
        var ordinal = reader.GetOrdinal(column);

        if (reader.IsDBNull(ordinal))
            return null;

        var value = reader.GetValue(ordinal);

        return value switch
        {
            DateTime dateTime => dateTime,
            string text => DateTime.Parse(text),
            _ => Convert.ToDateTime(value)
        };
    }

    private static long GetStableHash64(string input)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(input));

        return BitConverter.ToInt64(bytes, 0);
    }
}