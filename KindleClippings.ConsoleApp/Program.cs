using System.Text.Json;
using KindleClippings;
using KindleClippings.ConsoleApp;
using KindleClippings.ConsoleApp.Ai;

using var cancellationSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationSource.Cancel();
    Console.WriteLine("Interruzione richiesta: completo l'operazione corrente...");
};

try
{
    var arguments = CommandArguments.Parse(args);
    if (arguments.Command is "help" or "--help" or "-h")
    {
        PrintHelp();
        return;
    }

    if (arguments.Command is null ||
        arguments.Command.Equals("import", StringComparison.OrdinalIgnoreCase))
    {
        await ImportAndExportAsync(cancellationSource.Token);
        return;
    }

    var options = await AiDiscoveryOptions.LoadAsync(
        arguments.Get("config"),
        cancellationSource.Token);
    var repository = new ClippingDiscoveryRepository(options.ConnectionString);
    var corpus = await repository.LoadAsync(cancellationSource.Token);
    Console.WriteLine($"Caricate {corpus.Count:N0} citazioni esportabili.");

    using var ollama = new OllamaClient(
        options.OllamaBaseUrl,
        TimeSpan.FromMinutes(options.RequestTimeoutMinutes),
        options.ContextWindowTokens,
        options.MaxOutputTokens);

    switch (arguments.Command.ToLowerInvariant())
    {
        case "discover-taxonomy":
            await DiscoverTaxonomyAsync(arguments, options, corpus, ollama, cancellationSource.Token);
            break;

        case "compare-models":
            await CompareModelsAsync(arguments, options, corpus, ollama, cancellationSource.Token);
            break;

        default:
            throw new ArgumentException($"Comando sconosciuto: '{arguments.Command}'.");
    }
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Operazione annullata.");
    Environment.ExitCode = 2;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Errore: {exception.Message}");
    Environment.ExitCode = 1;
}

static async Task DiscoverTaxonomyAsync(
    CommandArguments arguments,
    AiDiscoveryOptions options,
    IReadOnlyCollection<AiClipping> corpus,
    OllamaClient ollama,
    CancellationToken cancellationToken)
{
    var model = arguments.Get("model") ?? options.Models[0];
    var output = ResolveOutputDirectory(arguments.Get("output") ?? options.OutputDirectory);
    Console.WriteLine($"Cartella report: {output}");
    var service = new TaxonomyDiscoveryService(ollama);
    var report = await service.DiscoverAsync(
        corpus,
        model,
        options.DiscoverySampleSize,
        options.DiscoveryBatchSize,
        options.MaxClippingCharacters,
        output,
        cancellationToken);
    var paths = await ReportWriter.WriteDiscoveryAsync(report, output, cancellationToken);

    Console.WriteLine($"Report JSON: {Path.GetFullPath(paths.JsonPath)}");
    Console.WriteLine($"Report leggibile: {Path.GetFullPath(paths.MarkdownPath)}");
}

static async Task CompareModelsAsync(
    CommandArguments arguments,
    AiDiscoveryOptions options,
    IReadOnlyCollection<AiClipping> corpus,
    OllamaClient ollama,
    CancellationToken cancellationToken)
{
    var taxonomyPath = arguments.Require("taxonomy");
    var variantName = arguments.Get("variant") ?? "balanced";
    var output = ResolveOutputDirectory(arguments.Get("output") ?? options.OutputDirectory);
    Console.WriteLine($"Cartella report: {output}");

    await using var taxonomyStream = File.OpenRead(taxonomyPath);
    var discovery = await JsonSerializer.DeserializeAsync<DiscoveryReport>(
        taxonomyStream,
        JsonDefaults.Options,
        cancellationToken)
        ?? throw new InvalidOperationException("Il report di discovery è vuoto.");
    var taxonomy = discovery.Taxonomies.Variants.SingleOrDefault(x =>
        x.Name.Equals(variantName, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"La variante '{variantName}' non esiste nel report.");

    var service = new ModelComparisonService(ollama);
    var report = await service.CompareAsync(
        corpus,
        discovery.SampleClippingIds,
        taxonomy,
        options.Models,
        options.ValidationSampleSize,
        Path.GetFullPath(taxonomyPath),
        discovery.Model,
        cancellationToken);
    var paths = await ReportWriter.WriteComparisonAsync(report, output, cancellationToken);

    Console.WriteLine($"Report JSON: {Path.GetFullPath(paths.JsonPath)}");
    Console.WriteLine($"Report leggibile: {Path.GetFullPath(paths.MarkdownPath)}");
}

static async Task ImportAndExportAsync(CancellationToken cancellationToken)
{
    var content = await File.ReadAllTextAsync(@".\My Clippings.txt", cancellationToken);
    var result = Parser.Parse(content);
    Console.WriteLine($"Parsed {result.Books.Count} books, {result.Clippings.Count} clippings");

    var importResult = await ClippingImporter.ImportAsync(result.Clippings.Values, cancellationToken);
    Console.WriteLine(
        $"Importazione completata: {importResult.Inserted} inseriti, {importResult.Updated} aggiornati.");

    var outputJsonFile = @"..\AlbertoBizzini.Web\wwwroot\data\clippings.json";
    var (count, actualOutputFile) = await ClippingJsonExporter.ExportAsync(
        outputJsonFile,
        cancellationToken);
    Console.WriteLine($"Exported {count:N0} clippings in '{actualOutputFile}'.");
}

static string ResolveOutputDirectory(string outputDirectory) =>
    Path.GetFullPath(
        Path.IsPathRooted(outputDirectory)
            ? outputDirectory
            : Path.Combine(AppContext.BaseDirectory, outputDirectory));

static void PrintHelp()
{
    Console.WriteLine("""
        Kindle Clippings Console

        Comandi:
          import
              Importa My Clippings.txt ed esporta clippings.json.

          discover-taxonomy [--model <nome>] [--output <cartella>] [--config <file>]
              Propone tre tassonomie usando il modello configurato o specificato.

          compare-models --taxonomy <discovery.json> [--variant balanced]
                         [--output <cartella>] [--config <file>]
              Confronta i modelli configurati su un campione non usato per la discovery.
        """);
}

internal sealed class CommandArguments
{
    private readonly Dictionary<string, string> _options;

    private CommandArguments(string? command, Dictionary<string, string> options)
    {
        Command = command;
        _options = options;
    }

    public string? Command { get; }

    public string? Get(string name) => _options.GetValueOrDefault(name);

    public string Require(string name) => Get(name)
        ?? throw new ArgumentException($"L'opzione --{name} è obbligatoria.");

    public static CommandArguments Parse(string[] args)
    {
        if (args.Length == 0)
            return new CommandArguments(null, new Dictionary<string, string>());

        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 1; index < args.Length; index += 2)
        {
            var option = args[index];
            if (!option.StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length)
                throw new ArgumentException($"Opzione non valida: '{option}'.");

            options.Add(option[2..], args[index + 1]);
        }

        return new CommandArguments(args[0], options);
    }
}
