namespace KindleClippings;

public class ParseResult
{
    public Dictionary<string, Clipping> Clippings { get; set; } = [];
    public List<Book> Books { get; set; } = [];
}
