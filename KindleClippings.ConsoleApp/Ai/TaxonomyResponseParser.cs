using System.Text.Json;

namespace KindleClippings.ConsoleApp.Ai;

internal static class TaxonomyResponseParser
{
    private static readonly HashSet<string> TopicCollectionNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "topics",
        "themes",
        "categories",
        "macroTopics",
        "macroThemes",
        "macro_temi"
    };

    public static TaxonomyVariant Parse(string json, string expectedName)
    {
        using var document = JsonDocument.Parse(json);
        var container = FindNamedVariant(document.RootElement, expectedName)
            ?? FindTopicContainer(document.RootElement);
        if (container is null)
        {
            throw new InvalidOperationException(
                $"La risposta per '{expectedName}' non contiene una collezione riconoscibile di temi. " +
                "La risposta grezza è stata salvata nella cartella AiReports.");
        }

        var topics = JsonSerializer.Deserialize<List<CandidateTopic>>(
            container.Topics.GetRawText(),
            JsonDefaults.Options) ?? [];

        return new TaxonomyVariant
        {
            Name = expectedName,
            Description = GetString(container.Parent, "description") ?? string.Empty,
            Topics = topics
        };
    }

    private static TopicContainer? FindNamedVariant(JsonElement element, string expectedName)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var found = FindNamedVariant(item, expectedName);
                if (found is not null)
                    return found;
            }

            return null;
        }

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(expectedName, StringComparison.OrdinalIgnoreCase))
            {
                var found = FindTopicContainer(property.Value);
                if (found is not null)
                    return found;
            }
        }

        var name = GetString(element, "name");
        if (name?.Equals(expectedName, StringComparison.OrdinalIgnoreCase) == true)
        {
            var found = FindTopicContainer(element);
            if (found is not null)
                return found;
        }

        foreach (var property in element.EnumerateObject())
        {
            var found = FindNamedVariant(property.Value, expectedName);
            if (found is not null)
                return found;
        }

        return null;
    }

    private static TopicContainer? FindTopicContainer(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
            return new TopicContainer(element, element);

        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in element.EnumerateObject())
        {
            if (TopicCollectionNames.Contains(property.Name) &&
                property.Value.ValueKind == JsonValueKind.Array)
            {
                return new TopicContainer(element, property.Value);
            }
        }

        foreach (var property in element.EnumerateObject())
        {
            var nested = FindTopicContainer(property.Value);
            if (nested is not null)
                return nested;
        }

        return null;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    private sealed record TopicContainer(JsonElement Parent, JsonElement Topics);
}
