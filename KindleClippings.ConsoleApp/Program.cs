using KindleClippings;

var content = File.ReadAllText(@".\My Clippings.txt");

var result = Parser.Parse(content);
Console.WriteLine($"Parsed {result.Books.Count} books, {result.Clippings.Count} clippings");

foreach(var book in result.Books.OrderBy(b => b.Clippings[0].AddedOn))
{
    Console.WriteLine($"========================");
    Console.WriteLine($"BOOK: {book}");
    Console.WriteLine($"1st clipping: {book.Clippings[0].AddedOn}");
    var count = 0;
    foreach (var c in book.Clippings)
    {
        Console.WriteLine($"\tCOUNT: {++count}");
        Console.WriteLine($"\tTYPE: {c.Type}");
        Console.WriteLine($"\tADDEDON: {c.AddedOn}");
        Console.WriteLine($"\tPAGE: {c.Page}");
        Console.WriteLine($"\tLOCATION: {c.StartLocation}-{c.EndLocation}");
        Console.WriteLine(c.Text);
        Console.WriteLine();
    }
}
