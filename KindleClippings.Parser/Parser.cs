using System.Globalization;
using System.Text.RegularExpressions;

namespace KindleClippings;

public static class Parser
{
    private static readonly Regex TitleAndBookRegex =
        new(@"^(?<Titolo>.+?)\s*(?:\([^)]+\)\s*)*(?:\((?<Autore>[^)]+)\))$",
            RegexOptions.IgnoreCase);

    private static readonly Regex PageRegex =
        new(@"pagina\s+(\d+)|page\s+(\d+)",
            RegexOptions.IgnoreCase);

    private static readonly Regex LocationRegex =
        new(@"posizione\s+(\d+)(?:-(\d+))?|location\s+(\d+)(?:-(\d+))?",
            RegexOptions.IgnoreCase);

    public static ParseResult Parse(string fileContent)
    {
        var clippings = new List<Clipping>();
        var books = new HashSet<Book>();

        var entries = fileContent
            .Split("==========", StringSplitOptions.RemoveEmptyEntries);

        foreach (var raw in entries)
        {
            var entry = raw.Trim();

            if (string.IsNullOrWhiteSpace(entry))
                continue;

            var lines = entry.Replace("\r", "")
                             .Split('\n');

            if (lines.Length < 2)
                continue;

            var titleLine = lines[0].Trim();
            var metadataLine = lines[1].Trim();

            string? text = null;

            if (lines.Length > 3)
            {
                text = string.Join(
                    Environment.NewLine,
                    lines.Skip(3)).Trim();

                text = Regex.Replace(text, @"\[▶\d+\]", "").Trim(); // remove metadata
            }

            if (string.IsNullOrWhiteSpace(text))
                continue;

            var parsedBook = ExtractBook(titleLine);
            var book = parsedBook;
            if (!books.TryGetValue(parsedBook, out book))
            {
                books.Add(book = parsedBook);
            }

            var parsedClipping = new Clipping
            {
                Book = parsedBook,
                Type = ParseType(metadataLine),
                Page = ParsePage(metadataLine),
                StartLocation = ParseStartLocation(metadataLine),
                EndLocation = ParseEndLocation(metadataLine),
                AddedOn = ParseDate(metadataLine),
                Text = text
            };

            clippings.Add(parsedClipping);
            book.Clippings.Add(parsedClipping);
        }

        return new ParseResult { Books = books.ToList(), Clippings = clippings };
    }

    private static Book ExtractBook(string line)
    {
        var match = TitleAndBookRegex.Match(line);

        if (!match.Success)
            return new Book { Title = line };

        return new Book
        {
            Title = match.Groups["Titolo"].Value,
            Author = match.Groups["Autore"].Value
        };
    }

    private static ClippingType ParseType(string metadata)
    {
        metadata = metadata.ToLowerInvariant();

        if (metadata.Contains("evidenziazione") ||
            metadata.Contains("highlight"))
            return ClippingType.Highlight;

        if (metadata.Contains("nota") ||
            metadata.Contains("note"))
            return ClippingType.Note;

        if (metadata.Contains("segnalibro") ||
            metadata.Contains("bookmark"))
            return ClippingType.Bookmark;

        return ClippingType.Unknown;
    }

    private static int? ParsePage(string metadata)
    {
        var match = PageRegex.Match(metadata);

        if (!match.Success)
            return null;

        var value = match.Groups[1].Success
            ? match.Groups[1].Value
            : match.Groups[2].Value;

        return int.Parse(value);
    }

    private static int? ParseStartLocation(string metadata)
    {
        var match = LocationRegex.Match(metadata);

        if (!match.Success)
            return null;

        var value = match.Groups[1].Success
            ? match.Groups[1].Value
            : match.Groups[3].Value;

        return int.Parse(value);
    }

    private static int? ParseEndLocation(string metadata)
    {
        var match = LocationRegex.Match(metadata);

        if (!match.Success)
            return null;

        var value = match.Groups[2].Success
            ? match.Groups[2].Value
            : match.Groups[4].Value;

        if (string.IsNullOrEmpty(value))
            return ParseStartLocation(metadata);

        return int.Parse(value);
    }

    private static DateTime? ParseDate(string metadata)
    {
        var idx = metadata.IndexOf("Aggiunto in data", StringComparison.OrdinalIgnoreCase);

        if (idx >= 0)
        {
            var date = metadata[(idx + "Aggiunto in data".Length)..].Trim();

            if (DateTime.TryParse(
                date,
                CultureInfo.GetCultureInfo("it-IT"),
                DateTimeStyles.None,
                out var dt))
                return dt;
        }

        idx = metadata.IndexOf("Added on", StringComparison.OrdinalIgnoreCase);

        if (idx >= 0)
        {
            var date = metadata[(idx + "Added on".Length)..].Trim();

            if (DateTime.TryParse(
                date,
                CultureInfo.GetCultureInfo("en-US"),
                DateTimeStyles.None,
                out var dt))
                return dt;
        }

        return null;
    }
}