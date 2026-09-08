using System.Security.Cryptography;
using System.Text;

namespace KindleClippings.ConsoleApp.Ai;

public static class DeterministicSampler
{
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
}
