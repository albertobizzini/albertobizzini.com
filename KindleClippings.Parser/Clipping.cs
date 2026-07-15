namespace KindleClippings;

public enum ClippingType
{
    Highlight,
    Note,
    Bookmark,
    Unknown
}

public class Clipping
{
    public Book Book {  get; init; }

    public ClippingType Type { get; init; }

    public int? Page { get; init; }

    public int? StartLocation { get; init; }

    public int? EndLocation { get; init; }

    public DateTime? AddedOn { get; init; }

    public string? Text { get; init; }
}