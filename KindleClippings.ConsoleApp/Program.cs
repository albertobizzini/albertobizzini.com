using KindleClippings;
using KindleClippings.Console;
using KindleClippings.ConsoleApp;
using Microsoft.Data.Sqlite;

var content = File.ReadAllText(@".\My Clippings.txt");

var result = Parser.Parse(content);
Console.WriteLine($"Parsed {result.Books.Count} books, {result.Clippings.Count} clippings");

var importResult = await ClippingImporter.ImportAsync(result.Clippings.Values);

Console.WriteLine(
    $"Importazione completata: " +
    $"{importResult.Inserted} inseriti, " +
    $"{importResult.Updated} aggiornati.");

Console.WriteLine("Esportazione in formato JSON per applicazione web...");
var outputJsonFile = @"..\AlbertoBizzini.Web\wwwroot\data\clippings.json";
var (count, actualOutputFile) = await ClippingJsonExporter.ExportAsync(outputJsonFile);
Console.WriteLine(
    $"Exported {count:N0} clippings in '{actualOutputFile}'.");


//var outputSqlLiteFile = @"..\AlbertoBizzini.Web\wwwroot\data\clippings.db";
//var count = await ClippingSqliteExporter.ExportAsync(outputSqlLiteFile);
//Console.WriteLine(
//    $"Exported {count:N0} clippings in '{outputSqlLiteFile}'.");

//var list = result.Clippings.Where(c => (c.Text?.Length ?? 0) < 10).ToList();
//foreach (var clip in list)
//{
//    Console.WriteLine($"BOOK: {clip.Book.Title} <{clip.Text}> ({clip.Text.Length})");
//}

//foreach (var book in result.Books.OrderBy(b => b.Clippings[0].AddedOn))
//{
//    Console.WriteLine($"========================");
//    Console.WriteLine($"BOOK: {book}");
//    Console.WriteLine($"1st clipping: {book.Clippings[0].AddedOn}");
//    var count = 0;
//    foreach (var c in book.Clippings.OrderBy(c => c.StartLocation))
//    {
//        Console.WriteLine($"\tCOUNT: {++count}");
//        Console.WriteLine($"\tID: {c.Id}");
//        Console.WriteLine($"\tTYPE: {c.Type}");
//        Console.WriteLine($"\tADDEDON: {c.AddedOn}");
//        Console.WriteLine($"\tPAGE: {c.Page}");
//        Console.WriteLine($"\tLOCATION: {c.StartLocation}-{c.EndLocation}");
//        Console.WriteLine(c.Text);
//        Console.WriteLine();
//    }
//}
