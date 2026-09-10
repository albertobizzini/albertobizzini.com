using ClosedXML.Excel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace KindleClippings.ConsoleApp;

public static class GooglePhotoAlbumJsonExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<(int count, string actualOutputFile)> ExportAsync(
        string excelFile,
        string jsonFile,
        CancellationToken cancellationToken = default,
        [CallerFilePath] string sourceFilePath = "")
    {
        excelFile = ResolvePath(excelFile, sourceFilePath);
        jsonFile = ResolvePath(jsonFile, sourceFilePath);

        using var workbook = new XLWorkbook(excelFile);
        var worksheet = workbook.Worksheet(1);
        var headerRow = worksheet.FirstRowUsed()
            ?? throw new InvalidOperationException("Il file Excel non contiene intestazioni.");
        var columns = headerRow.CellsUsed().ToDictionary(
            cell => cell.GetString().Trim(),
            cell => cell.Address.ColumnNumber,
            StringComparer.OrdinalIgnoreCase);

        var albums = new List<GooglePhotoAlbum>();
        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var publicUrl = Text(row, columns, "URL pubblico");
            if (string.IsNullOrWhiteSpace(publicUrl))
                continue;

            albums.Add(new GooglePhotoAlbum
            {
                DateFrom = Date(row, columns, "Data da"),
                DateTo = Date(row, columns, "Data a"),
                Title = Text(row, columns, "Titolo") ?? string.Empty,
                Continent = Text(row, columns, "Continente"),
                Country = Text(row, columns, "Luogo (nazione)"),
                CountryEnglish = Text(row, columns, "Luogo (nazione) - English"),
                Place = Text(row, columns, "Luogo (città)"),
                PlaceEnglish = Text(row, columns, "Luogo (città)  - English"),
                Latitude = Number(row, columns, "Luogo (Latitudine)"),
                Longitude = Number(row, columns, "Luogo (Longitudine)"),
                PublicUrl = publicUrl
            });
        }

        Directory.CreateDirectory(Path.GetDirectoryName(jsonFile)!);
        await using var stream = File.Create(jsonFile);
        await JsonSerializer.SerializeAsync(stream, albums, JsonOptions, cancellationToken);
        return (albums.Count, jsonFile);
    }

    private static string ResolvePath(string path, string sourceFilePath) =>
        Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFilePath)!, path));

    private static string? Text(IXLRow row, IReadOnlyDictionary<string, int> columns, string name)
    {
        EnsureColumn(columns, name);
        var value = row.Cell(columns[name]).GetString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static double? Number(IXLRow row, IReadOnlyDictionary<string, int> columns, string name)
    {
        EnsureColumn(columns, name);
        var cell = row.Cell(columns[name]);
        if (cell.IsEmpty())
            return null;
        if (cell.TryGetValue<double>(out var number))
            return number;
        return double.TryParse(cell.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number)
            ? number
            : null;
    }

    private static DateOnly? Date(IXLRow row, IReadOnlyDictionary<string, int> columns, string name)
    {
        EnsureColumn(columns, name);
        var cell = row.Cell(columns[name]);
        if (cell.IsEmpty())
            return null;
        if (cell.TryGetValue<DateTime>(out var date))
            return DateOnly.FromDateTime(date);
        return DateOnly.TryParse(cell.GetString(), CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static void EnsureColumn(IReadOnlyDictionary<string, int> columns, string name)
    {
        if (!columns.ContainsKey(name))
            throw new InvalidOperationException($"Colonna obbligatoria non trovata: '{name}'.");
    }
}

public sealed class GooglePhotoAlbum
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public required string Title { get; init; }
    public string? Continent { get; init; }
    public string? Country { get; init; }
    public string? CountryEnglish { get; init; }
    public string? Place { get; init; }
    public string? PlaceEnglish { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public required string PublicUrl { get; init; }
}
