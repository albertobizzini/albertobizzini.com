using System.Text.RegularExpressions;

namespace KindleClippings.ConsoleApp.Ai;

internal static partial class LanguageDetector
{
    private static readonly HashSet<string> ItalianWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "che", "della", "delle", "degli", "non", "per", "una", "con", "sono", "come",
        "nel", "nella", "alla", "anche", "più", "essere", "questo", "questa", "quando"
    };

    private static readonly HashSet<string> EnglishWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "that", "not", "for", "with", "are", "as", "this", "when",
        "from", "have", "was", "but", "you", "your", "into", "than", "their", "what"
    };

    [GeneratedRegex(@"[\p{L}']+")]
    private static partial Regex Words();

    public static string Detect(string text)
    {
        var words = Words().Matches(text).Select(match => match.Value);
        var italian = 0;
        var english = 0;

        foreach (var word in words)
        {
            if (ItalianWords.Contains(word)) italian++;
            if (EnglishWords.Contains(word)) english++;
        }

        return english > italian ? "en" : "it";
    }
}
