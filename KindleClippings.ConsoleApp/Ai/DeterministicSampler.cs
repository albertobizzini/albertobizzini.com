using System.Security.Cryptography;
using System.Text;

namespace KindleClippings.ConsoleApp.Ai;

public static class DeterministicSampler
{
    public static List<AiClipping> SelectRepresentative(
        IReadOnlyCollection<AiClipping> source,
        int count,
        string seed)
    {
        if (count <= 0 || source.Count == 0)
            return [];

        var target = Math.Min(count, source.Count);
        var breadthCount = Math.Min(target / 4, source
            .Select(BookKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count());
        var selected = source
            .GroupBy(BookKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.MinBy(x => Hash(seed + "|breadth", x.Id))!)
            .OrderBy(x => Hash(seed + "|books", x.Id))
            .Take(breadthCount)
            .ToList();
        var selectedIds = selected.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);

        selected.AddRange(source
            .Where(x => !selectedIds.Contains(x.Id))
            .OrderBy(x => Hash(seed + "|proportional", x.Id))
            .Take(target - selected.Count));

        return selected.OrderBy(x => Hash(seed + "|order", x.Id)).ToList();
    }

    public static List<AiClipping> Select(
        IReadOnlyCollection<AiClipping> source,
        int count,
        string seed,
        ISet<string>? excludedIds = null)
    {
        if (count <= 0 || source.Count == 0)
            return [];

        var books = source
            .Where(x => excludedIds is null || !excludedIds.Contains(x.Id))
            .GroupBy(x => $"{x.Author}\n{x.Title}")
            .Select(group => new Queue<AiClipping>(group.OrderBy(x => Hash(seed, x.Id))))
            .OrderByDescending(queue => queue.Count)
            .ToList();

        var selected = new List<AiClipping>(Math.Min(count, source.Count));
        while (selected.Count < count && books.Count > 0)
        {
            foreach (var book in books.ToList())
            {
                if (selected.Count == count)
                    break;

                selected.Add(book.Dequeue());
                if (book.Count == 0)
                    books.Remove(book);
            }
        }

        return selected;
    }

    private static string Hash(string seed, string id)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{seed}|{id}"));
        return Convert.ToHexString(bytes);
    }

    private static string BookKey(AiClipping clipping) =>
        $"{clipping.Author}\n{clipping.Title}";
}
