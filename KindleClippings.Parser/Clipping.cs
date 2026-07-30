using System.Text.Json.Serialization;

namespace KindleClippings;

public enum ClippingType
{
    // NB  non cambiare! vedi ClippingIdGenerator.CreateId
    Highlight,
    Note,
    Bookmark,
    Unknown
}

public class Clipping
{
    public string Id { get; set; }
    public Book Book {  get; init; }

    public ClippingType Type { get; init; }

    public int? Page { get; init; }

    public int? StartLocation { get; init; }

    public int? EndLocation { get; init; }

    public DateTime? AddedOn { get; init; }

    public string? Text { get; init; }

    [JsonIgnore]
    public string? QuotedText => !string.IsNullOrWhiteSpace(Text) ? $"«{Text}»" : string.Empty;
}