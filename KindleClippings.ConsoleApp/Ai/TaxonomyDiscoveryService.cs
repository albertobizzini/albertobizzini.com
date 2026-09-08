using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace KindleClippings.ConsoleApp.Ai;

public sealed class TaxonomyDiscoveryService(OllamaClient ollamaClient)
{
    private const string DiscoverySystemPrompt = """
        Sei un tassonomista editoriale. Analizza citazioni italiane e inglesi e proponi macro-temi
        utili alla navigazione di un archivio pubblico. Restituisci esclusivamente JSON valido.
        I codici devono essere snake_case inglesi, stabili e concisi. Evita categorie duplicate,
        nomi di autori, titoli di libri e temi applicabili a una sola citazione.
        """;

    private const string ConsolidationSystemPrompt = """
        Sei un tassonomista editoriale. Consolida temi candidati derivati da citazioni italiane e
        inglesi. Restituisci esclusivamente JSON valido. Crea esattamente tre varianti chiamate
        compact, balanced e detailed. Compact deve avere 10-12 temi, balanced 15-18 temi e
        detailed 20-25 temi. Ogni tema deve avere code, nameIt, nameEn, descriptionIt,
        descriptionEn ed exampleClippingIds. I codici devono essere snake_case inglesi e univoci.
        Mantieni solo gli ID di esempio ricevuti, massimo cinque per tema. Le descrizioni devono
        chiarire i confini del tema e ridurre le sovrapposizioni.
        """;

    public async Task<DiscoveryReport> DiscoverAsync(
        IReadOnlyCollection<AiClipping> corpus,
        string model,
        int sampleSize,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var sample = DeterministicSampler.Select(corpus, sampleSize, "taxonomy-discovery-v1");
        var candidates = new List<CandidateTopic>();
        var batches = sample.Chunk(batchSize).ToList();

        for (var index = 0; index < batches.Count; index++)
        {
            Console.WriteLine($"Discovery batch {index + 1}/{batches.Count}...");
            var prompt = BuildDiscoveryPrompt(batches[index]);
            var response = await ollamaClient.GenerateJsonAsync<CandidateTopicSet>(
                model,
                DiscoverySystemPrompt,
                prompt,
                cancellationToken);
            candidates.AddRange(response.Value.Topics);
        }

        Console.WriteLine($"Consolidamento di {candidates.Count} temi candidati...");
        var consolidation = await ollamaClient.GenerateJsonAsync<TaxonomyVariants>(
            model,
            ConsolidationSystemPrompt,
            BuildConsolidationPrompt(candidates),
            cancellationToken);

        TaxonomyValidator.Validate(consolidation.Value);

        return new DiscoveryReport
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Model = model,
            CorpusSize = corpus.Count,
            SampleSize = sample.Count,
            BatchSize = batchSize,
            DurationMilliseconds = stopwatch.ElapsedMilliseconds,
            SampleClippingIds = sample.Select(x => x.Id).ToList(),
            Taxonomies = consolidation.Value
        };
    }

    private static string BuildDiscoveryPrompt(IEnumerable<AiClipping> clippings)
    {
        var input = clippings.Select(x => new
        {
            x.Id,
            x.Language,
            x.Text
        });

        return """
            Proponi da 5 a 10 macro-temi rappresentati nel seguente batch. Un tema può coprire più
            citazioni. Restituisci un oggetto con la proprietà topics. Ogni elemento deve contenere:
            code, nameIt, nameEn, descriptionIt, descriptionEn ed exampleClippingIds (massimo 5).

            Citazioni:
            """ + JsonSerializer.Serialize(input, JsonDefaults.Options);
    }

    private static string BuildConsolidationPrompt(IEnumerable<CandidateTopic> candidates)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Temi candidati da consolidare:");
        builder.Append(JsonSerializer.Serialize(candidates, JsonDefaults.Options));
        return builder.ToString();
    }
}
