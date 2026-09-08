using System.Text.Json;

namespace KindleClippings.ConsoleApp.Ai;

public sealed class AiDiscoveryOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public string OllamaBaseUrl { get; init; } = "http://localhost:11434";
    public List<string> Models { get; init; } = [];
    public int DiscoverySampleSize { get; init; } = 500;
    public int ValidationSampleSize { get; init; } = 150;
    public int DiscoveryBatchSize { get; init; } = 25;
    public int RequestTimeoutMinutes { get; init; } = 10;
    public int ContextWindowTokens { get; init; } = 16384;
    public int MaxOutputTokens { get; init; } = 4096;
    public int MaxClippingCharacters { get; init; } = 800;
    public string OutputDirectory { get; init; } = "AiReports";

    public static async Task<AiDiscoveryOptions> LoadAsync(
        string? path,
        CancellationToken cancellationToken)
    {
        path ??= FindDefaultConfigurationFile();

        await using var stream = File.OpenRead(path);
        var root = await JsonSerializer.DeserializeAsync<ConfigurationRoot>(
            stream,
            JsonDefaults.Options,
            cancellationToken);

        var options = root?.AiDiscovery
            ?? throw new InvalidOperationException(
                $"La sezione AiDiscovery non è presente in '{path}'.");

        options.Validate();
        return options;
    }

    private static string FindDefaultConfigurationFile()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.CurrentDirectory, "appsettings.json"),
            Path.Combine(AppContext.BaseDirectory, "appsettings.json")
        };

        return candidates.FirstOrDefault(File.Exists)
            ?? throw new FileNotFoundException(
                "Impossibile trovare appsettings.json. Usa --config <percorso>.");
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException("AiDiscovery.ConnectionString è obbligatoria.");

        if (!Uri.TryCreate(OllamaBaseUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException("AiDiscovery.OllamaBaseUrl non è un URL valido.");

        if (Models.Count < 2 || Models.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("Configura almeno due modelli in AiDiscovery.Models.");

        if (DiscoverySampleSize <= 0 || ValidationSampleSize <= 0 || DiscoveryBatchSize <= 0)
            throw new InvalidOperationException("Le dimensioni dei campioni e dei batch devono essere positive.");

        if (RequestTimeoutMinutes <= 0)
            throw new InvalidOperationException("RequestTimeoutMinutes deve essere positivo.");

        if (ContextWindowTokens <= 0 || MaxOutputTokens <= 0 || MaxClippingCharacters <= 0)
            throw new InvalidOperationException(
                "ContextWindowTokens, MaxOutputTokens e MaxClippingCharacters devono essere positivi.");
    }

    private sealed class ConfigurationRoot
    {
        public AiDiscoveryOptions? AiDiscovery { get; init; }
    }
}
