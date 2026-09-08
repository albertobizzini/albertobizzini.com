using System.Text;
using System.Text.Json;

namespace KindleClippings.ConsoleApp.Ai;

public static class ReportWriter
{
    public static async Task<(string JsonPath, string MarkdownPath)> WriteDiscoveryAsync(
        DiscoveryReport report,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        var stamp = report.GeneratedAt.ToString("yyyyMMdd-HHmmss");
        var baseName = $"taxonomy-discovery-{SafeName(report.Model)}-{stamp}";
        var jsonPath = Path.Combine(outputDirectory, baseName + ".json");
        var markdownPath = Path.Combine(outputDirectory, baseName + ".md");

        await WriteJsonAsync(jsonPath, report, cancellationToken);
        await File.WriteAllTextAsync(
            markdownPath,
            BuildDiscoveryMarkdown(report),
            cancellationToken);
        return (jsonPath, markdownPath);
    }

    public static async Task<(string JsonPath, string MarkdownPath)> WriteComparisonAsync(
        ComparisonReport report,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        var stamp = report.GeneratedAt.ToString("yyyyMMdd-HHmmss");
        var baseName = $"model-comparison-{report.TaxonomyVariant}-{stamp}";
        var jsonPath = Path.Combine(outputDirectory, baseName + ".json");
        var markdownPath = Path.Combine(outputDirectory, baseName + ".md");

        await WriteJsonAsync(jsonPath, report, cancellationToken);
        await File.WriteAllTextAsync(
            markdownPath,
            BuildComparisonMarkdown(report),
            cancellationToken);
        return (jsonPath, markdownPath);
    }

    private static async Task WriteJsonAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            value,
            JsonDefaults.Options,
            cancellationToken);
    }

    private static string BuildDiscoveryMarkdown(DiscoveryReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Proposte di tassonomia");
        builder.AppendLine();
        builder.AppendLine($"- Modello: `{report.Model}`");
        builder.AppendLine($"- Corpus: {report.CorpusSize:N0} citazioni");
        builder.AppendLine($"- Campione: {report.SampleSize:N0} citazioni");
        builder.AppendLine($"- Durata: {TimeSpan.FromMilliseconds(report.DurationMilliseconds):g}");

        foreach (var variant in report.Taxonomies.Variants)
        {
            builder.AppendLine();
            builder.AppendLine($"## {variant.Name} ({variant.Topics.Count} temi)");
            builder.AppendLine();
            builder.AppendLine(variant.Description);
            builder.AppendLine();
            builder.AppendLine("| Codice | Nome italiano | Descrizione | Esempi |");
            builder.AppendLine("|---|---|---|---|");
            foreach (var topic in variant.Topics)
            {
                builder.AppendLine(
                    $"| `{Escape(topic.Code)}` | {Escape(topic.NameIt)} | {Escape(topic.DescriptionIt)} | " +
                    $"{Escape(string.Join(", ", topic.ExampleClippingIds))} |");
            }
        }

        return builder.ToString();
    }

    private static string BuildComparisonMarkdown(ComparisonReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Confronto anonimo dei modelli");
        builder.AppendLine();
        builder.AppendLine($"- Tassonomia: `{report.TaxonomyVariant}`");
        builder.AppendLine($"- Campione di validazione: {report.SampleSize:N0}");
        builder.AppendLine();
        builder.AppendLine("Per ogni citazione indica: **A**, **B**, **equivalenti** o **entrambi errati**.");

        for (var index = 0; index < report.Items.Count; index++)
        {
            var item = report.Items[index];
            builder.AppendLine();
            builder.AppendLine($"## {index + 1}. {item.Clipping.Id}");
            builder.AppendLine();
            builder.AppendLine($"> {item.Clipping.Text.ReplaceLineEndings(" ")}");

            for (var resultIndex = 0; resultIndex < item.Results.Count; resultIndex++)
            {
                var label = (char)('A' + resultIndex);
                var result = item.Results[resultIndex];
                builder.AppendLine();
                builder.AppendLine($"### Risultato {label}");
                if (!result.IsValid || result.Classification is null)
                {
                    builder.AppendLine($"Non valido: `{result.Error}`");
                    continue;
                }

                var classification = result.Classification;
                builder.AppendLine($"- Principale: {Format(classification.PrimaryTopic)}");
                builder.AppendLine($"- Secondari: {string.Join(", ", classification.SecondaryTopics.Select(Format))}");
                builder.AppendLine($"- Aforisma: {classification.AphorismScore:F2}");
                builder.AppendLine($"- Dipendenza dal contesto: {classification.ContextDependencyScore:F2}");
            }

            builder.AppendLine();
            builder.AppendLine("**Valutazione:** _da compilare_");
        }

        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine("## Chiave dei modelli");
        for (var index = 0; index < report.Models.Count; index++)
            builder.AppendLine($"- {(char)('A' + index)}: `{report.Models[index]}`");

        return builder.ToString();
    }

    private static string Format(TopicAssignment? assignment) =>
        assignment is null ? "nessuno" : $"`{assignment.Code}` ({assignment.Score:F2})";

    private static string Escape(string value) => value.Replace("|", "\\|").ReplaceLineEndings(" ");

    private static string SafeName(string value) =>
        string.Concat(value.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '-' : character));
}
