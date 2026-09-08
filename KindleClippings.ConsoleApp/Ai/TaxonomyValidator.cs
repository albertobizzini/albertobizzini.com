namespace KindleClippings.ConsoleApp.Ai;

internal static class TaxonomyValidator
{
    public static void Validate(TaxonomyVariants taxonomies)
    {
        if (taxonomies.Variants.Count != 3)
            throw new InvalidOperationException("Il modello deve restituire esattamente tre tassonomie.");

        var expected = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "compact", "balanced", "detailed" };

        foreach (var variant in taxonomies.Variants)
        {
            if (!expected.Contains(variant.Name))
                throw new InvalidOperationException($"Variante sconosciuta: '{variant.Name}'.");

            ValidateVariant(variant, variant.Name);
        }
    }

    public static void ValidateVariant(
        TaxonomyVariant variant,
        string expectedName)
    {
        if (variant.Topics.Count == 0)
            throw new InvalidOperationException(
                $"La variante '{expectedName}' non contiene temi. " +
                "Verifica la risposta del modello o prova un modello differente.");

        ValidateTopics(variant.Topics);
    }

    public static void ValidateClassification(
        ClippingClassification classification,
        IReadOnlySet<string> allowedTopics)
    {
        if (classification.SecondaryTopics.Count > 2)
            throw new InvalidOperationException("Sono consentiti al massimo due temi secondari.");

        var assignments = classification.SecondaryTopics
            .Concat(classification.PrimaryTopic is null ? [] : [classification.PrimaryTopic])
            .ToList();

        if (assignments.Any(x => !allowedTopics.Contains(x.Code)))
            throw new InvalidOperationException("La risposta contiene un tema non presente nella tassonomia.");

        if (assignments.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != assignments.Count)
            throw new InvalidOperationException("La risposta contiene temi duplicati.");

        if (assignments.Any(x => !IsScore(x.Score)) ||
            !IsScore(classification.AphorismScore) ||
            !IsScore(classification.ContextDependencyScore))
        {
            throw new InvalidOperationException("Tutti i punteggi devono essere compresi fra 0 e 1.");
        }
    }

    private static void ValidateTopics(IReadOnlyCollection<CandidateTopic> topics)
    {
        if (topics.Any(x =>
                string.IsNullOrWhiteSpace(x.Code) ||
                string.IsNullOrWhiteSpace(x.NameIt) ||
                string.IsNullOrWhiteSpace(x.NameEn) ||
                string.IsNullOrWhiteSpace(x.DescriptionIt) ||
                string.IsNullOrWhiteSpace(x.DescriptionEn)))
        {
            throw new InvalidOperationException("Ogni tema deve avere codice, nomi e descrizioni.");
        }

        if (topics.Select(x => x.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count() != topics.Count)
            throw new InvalidOperationException("I codici dei temi devono essere univoci.");
    }

    private static bool IsScore(double score) => score is >= 0 and <= 1;
}
