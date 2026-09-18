using ClosedXML.Excel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace KindleClippings.ConsoleApp;

public static class GooglePhotoAlbumJsonExporter
{
    private const string AlbumsSheet = "Albums";
    private const string LocationsSheet = "AlbumLocations";

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
        var albumsSheet = RequiredWorksheet(workbook, AlbumsSheet);
        var locationsSheet = RequiredWorksheet(workbook, LocationsSheet);
        var albumColumns = Columns(albumsSheet, [
            "AlbumId", "Data da", "Data a", "Titolo", "URL pubblico", "URL album"]);
        var locationColumns = Columns(locationsSheet, [
            "AlbumId", "Order", "Continente", "Luogo (nazione)",
            "Luogo (nazione) - English", "Luogo (città)", "Luogo (città) - English",
            "Luogo (Latitudine)", "Luogo (Longitudine)"]);

        var albumsById = new Dictionary<string, GooglePhotoAlbum>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in DataRows(albumsSheet))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = RequiredText(row, albumColumns, "AlbumId", AlbumsSheet);
            if (!albumsById.TryAdd(id, new GooglePhotoAlbum
                {
                    Id = id,
                    DateFrom = Date(row, albumColumns, "Data da"),
                    DateTo = Date(row, albumColumns, "Data a"),
                    Title = Text(row, albumColumns, "Titolo") ?? string.Empty,
                    PublicUrl = Text(row, albumColumns, "URL pubblico") ?? string.Empty,
                    Locations = []
                }))
                throw RowError(AlbumsSheet, row, $"AlbumId duplicato: '{id}'.");
        }

        var ordersByAlbum = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in DataRows(locationsSheet))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var albumId = RequiredText(row, locationColumns, "AlbumId", LocationsSheet);
            if (!albumsById.TryGetValue(albumId, out var album))
                throw RowError(LocationsSheet, row, $"AlbumId inesistente: '{albumId}'.");

            var order = RequiredInteger(row, locationColumns, "Order", LocationsSheet);
            if (!ordersByAlbum.TryGetValue(albumId, out var orders))
                ordersByAlbum.Add(albumId, orders = []);
            if (!orders.Add(order))
                throw RowError(LocationsSheet, row, $"Order {order} duplicato per l'album '{albumId}'.");

            var latitude = Number(row, locationColumns, "Luogo (Latitudine)", LocationsSheet);
            var longitude = Number(row, locationColumns, "Luogo (Longitudine)", LocationsSheet);
            if (latitude.HasValue != longitude.HasValue)
                throw RowError(LocationsSheet, row, "Latitudine e longitudine devono essere entrambe presenti oppure entrambe vuote.");
            if (latitude is < -90 or > 90)
                throw RowError(LocationsSheet, row, $"Latitudine fuori dall'intervallo -90…90: {latitude}.");
            if (longitude is < -180 or > 180)
                throw RowError(LocationsSheet, row, $"Longitudine fuori dall'intervallo -180…180: {longitude}.");

            album.Locations.Add(new GooglePhotoAlbumLocation
            {
                Order = order,
                Continent = Text(row, locationColumns, "Continente"),
                Country = Text(row, locationColumns, "Luogo (nazione)"),
                CountryEnglish = Text(row, locationColumns, "Luogo (nazione) - English"),
                Place = Text(row, locationColumns, "Luogo (città)"),
                PlaceEnglish = Text(row, locationColumns, "Luogo (città) - English"),
                Latitude = latitude,
                Longitude = longitude
            });
        }

        var albums = albumsById.Values
            .Where(album => !string.IsNullOrWhiteSpace(album.PublicUrl))
            .Select(album => album with { Locations = album.Locations.OrderBy(location => location.Order).ToList() })
            .ToList();

        Directory.CreateDirectory(Path.GetDirectoryName(jsonFile)!);
        await using var stream = File.Create(jsonFile);
        await JsonSerializer.SerializeAsync(stream, albums, JsonOptions, cancellationToken);
        return (albums.Count, jsonFile);
    }

    private static IXLWorksheet RequiredWorksheet(XLWorkbook workbook, string name) =>
        workbook.TryGetWorksheet(name, out var worksheet)
            ? worksheet
            : throw new InvalidOperationException($"Foglio obbligatorio non trovato: '{name}'.");

    private static Dictionary<string, int> Columns(IXLWorksheet worksheet, string[] required)
    {
        var header = worksheet.FirstRowUsed()
            ?? throw new InvalidOperationException($"Il foglio '{worksheet.Name}' non contiene intestazioni.");
        var columns = header.CellsUsed().ToDictionary(
            cell => cell.GetString().Trim(), cell => cell.Address.ColumnNumber,
            StringComparer.OrdinalIgnoreCase);
        var missing = required.Where(name => !columns.ContainsKey(name)).ToArray();
        if (missing.Length > 0)
            throw new InvalidOperationException(
                $"Colonne obbligatorie mancanti nel foglio '{worksheet.Name}': {string.Join(", ", missing.Select(x => $"'{x}'"))}.");
        return columns;
    }

    private static IEnumerable<IXLRow> DataRows(IXLWorksheet worksheet) =>
        worksheet.RowsUsed().Skip(1).Where(row => !row.IsEmpty());

    private static string ResolvePath(string path, string sourceFilePath) => Path.IsPathRooted(path)
        ? path
        : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceFilePath)!, path));

    private static string RequiredText(IXLRow row, IReadOnlyDictionary<string, int> columns, string name, string sheet) =>
        Text(row, columns, name) ?? throw RowError(sheet, row, $"'{name}' è obbligatorio.");

    private static string? Text(IXLRow row, IReadOnlyDictionary<string, int> columns, string name)
    {
        var value = row.Cell(columns[name]).GetString().Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static int RequiredInteger(IXLRow row, IReadOnlyDictionary<string, int> columns, string name, string sheet)
    {
        var cell = row.Cell(columns[name]);
        if (cell.TryGetValue<int>(out var value))
            return value;
        throw RowError(sheet, row, $"'{name}' deve contenere un numero intero.");
    }

    private static double? Number(IXLRow row, IReadOnlyDictionary<string, int> columns, string name, string sheet)
    {
        var cell = row.Cell(columns[name]);
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<double>(out var number)) return number;
        if (double.TryParse(cell.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return number;
        throw RowError(sheet, row, $"'{name}' deve contenere un numero valido.");
    }

    private static DateOnly? Date(IXLRow row, IReadOnlyDictionary<string, int> columns, string name)
    {
        var cell = row.Cell(columns[name]);
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue<DateTime>(out var date)) return DateOnly.FromDateTime(date);
        return DateOnly.TryParse(cell.GetString(), CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static InvalidOperationException RowError(string sheet, IXLRow row, string message) =>
        new($"Foglio '{sheet}', riga {row.RowNumber()}: {message}");
}

public sealed record GooglePhotoAlbum
{
    public required string Id { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public required string Title { get; init; }
    public required string PublicUrl { get; init; }
    public required List<GooglePhotoAlbumLocation> Locations { get; init; }
}

public sealed class GooglePhotoAlbumLocation
{
    public int Order { get; init; }
    public string? Continent { get; init; }
    public string? Country { get; init; }
    public string? CountryEnglish { get; init; }
    public string? Place { get; init; }
    public string? PlaceEnglish { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}
