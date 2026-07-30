using Blake3;
using KindleClippings;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

//namespace KindleClippings.Parser

public static partial class ClippingIdGenerator
{
    private const string Base62Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpace();

    /// <summary>
    /// Restituisce un ID Base62 di 13 caratteri (80 bit).
    /// </summary>
    public static string CreateId(Clipping clipping)
    {
        var normalized = string.Join("|",
            Normalize(clipping.Book.Title),
            Normalize(clipping.Book.Author ?? string.Empty),
            Normalize(clipping.Type.ToString()),
            clipping.StartLocation.ToString(),
            clipping.EndLocation?.ToString() ?? "",
            Normalize(clipping.Text));

        byte[] bytes = Encoding.UTF8.GetBytes(normalized);

        byte[] shortHash = Hasher.Hash(bytes)
                                 .AsSpan()[..10]
                                 .ToArray();

        return ToBase62(shortHash);
    }

    private static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return WhiteSpace().Replace(value.Normalize(NormalizationForm.FormC).Trim(), " ");
    }

    private static string ToBase62(ReadOnlySpan<byte> bytes)
    {
        // Converte il byte[] in BigInteger positivo
        var buffer = new byte[bytes.Length + 1];
        bytes.CopyTo(buffer);

        var value = new System.Numerics.BigInteger(buffer);

        if (value == 0)
            return "0";

        var sb = new StringBuilder();

        while (value > 0)
        {
            value = System.Numerics.BigInteger.DivRem(value, 62, out var rem);
            sb.Insert(0, Base62Alphabet[(int)rem]);
        }

        return sb.ToString();
    }

}