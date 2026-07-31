using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace KindleClippings;

public class Book : IEquatable<Book>
{

    public string Title { get; init; } = "";

    public string? Author { get; init; }

    public List<Clipping> Clippings { get; } = new();

    public override string ToString() => TitleAndAuthor;

    [JsonIgnore]
    public string TitleAndAuthor => Author is null
            ? Title
            : $"{Title} ({Author})";

    #region IEquatable<KindleBook>
    public bool Equals(Book? other)
    {
        if (other is null) return false;
        return TitleAndAuthor == other.TitleAndAuthor;
    }
    public override bool Equals(object? obj) => Equals(obj as Book);
    public override int GetHashCode() => TitleAndAuthor.GetHashCode();
    #endregion
}
