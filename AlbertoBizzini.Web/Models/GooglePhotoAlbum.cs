namespace AlbertoBizzini.Web.Models;

public sealed class GooglePhotoAlbum
{
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Continent { get; init; }
    public string? Country { get; init; }
    public string? CountryEnglish { get; init; }
    public string? Place { get; init; }
    public string? PlaceEnglish { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string PublicUrl { get; init; } = string.Empty;
}
