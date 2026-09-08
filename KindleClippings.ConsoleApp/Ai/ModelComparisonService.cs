using System.Diagnostics;
using System.Text;

namespace KindleClippings.ConsoleApp.Ai;

public sealed class ModelComparisonService(OllamaClient ollamaClient)
{
    private const string SystemPrompt = """
        Sei un classificatore editoriale di citazioni italiane e inglesi. Restituisci esclusivamente
        JSON valido con primaryTopic (oggetto code/score oppure null), secondaryTopics (massimo due
        oggetti code/score), aphorismScore e contextDependencyScore. Usa solo codici presenti nella
        tassonomia. Non forzare un tema se il testo non offre informazioni sufficienti. Un aforisma
        è conciso, memorabile, generalizzabile e comprensibile senza contesto. ContextDependencyScore
        misura quanto il frammento, considerato da solo, dipenda da testo precedente o omesso.
        Tutti gli score devono essere numeri fra 0 e 1.
        """;

    public async Task<ComparisonReport> CompareAsync(
        IReadOnlyCollection<AiClipping> corpus,
        IReadOnlyCollection<string> discoverySampleIds,
        TaxonomyVariant taxonomy,
        IReadOnlyList<string> models,
        int sampleSize,
        string taxonomySource,
        CancellationToken cancellationToken)
    {
        var excluded = discoverySampleIds.ToHashSet(StringComparer.Ordinal);
        var sample = DeterministicSampler.Select(
            corpus,
            sampleSize,
            "model-comparison-v1",
            excluded);
        var allowedTopics = taxonomy.Topics
            .Select(x => x.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var items = new List<ComparisonItem>(sample.Count);

        for (var clippingIndex = 0; clippingIndex < sample.Count; clippingIndex++)
        {
            var clipping = sample[clippingIndex];
            var item = new ComparisonItem { Clipping = clipping };
            Console.WriteLine($"Citazione {clippingIndex + 1}/{sample.Count} ({clipping.Id})...");

            foreach (var model in models)
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    var response = await ollamaClient.GenerateJsonAsync<ClippingClassification>(
                        model,
                        SystemPrompt,
                        BuildPrompt(clipping, taxonomy),
                        cancellationToken,
                        OllamaJsonSchemas.Classification(allowedTopics));
                    TaxonomyValidator.ValidateClassification(response.Value, allowedTopics);
                    item.Results.Add(new ModelClassificationResult
                    {
                        Model = model,
                        IsValid = true,
                        DurationMilliseconds = response.DurationMilliseconds,
                        Classification = response.Value
                    });
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    item.Results.Add(new ModelClassificationResult
                    {
                        Model = model,
                        IsValid = false,
                        DurationMilliseconds = stopwatch.ElapsedMilliseconds,
                        Error = exception.Message
                    });
                }
            }

            items.Add(item);
        }

        return new ComparisonReport
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            TaxonomySource = taxonomySource,
            TaxonomyVariant = taxonomy.Name,
            Models = models.ToList(),
            CorpusSize = corpus.Count,
            SampleSize = sample.Count,
            Items = items
        };
    }

    private static string BuildPrompt(AiClipping clipping, TaxonomyVariant taxonomy)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Tassonomia consentita:");
        foreach (var topic in taxonomy.Topics)
            builder.AppendLine($"- {topic.Code}: {topic.DescriptionIt} / {topic.DescriptionEn}");

        builder.AppendLine();
        builder.AppendLine($"Lingua stimata: {clipping.Language}");
        builder.AppendLine("Citazione:");
        builder.AppendLine(clipping.Text);
        return builder.ToString();
    }
}
