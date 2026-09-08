using System.Globalization;
using System.Text;
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

        var topics = ParseTopics(container.Topics)
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();

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
        {
            if (IsTopicArray(element))
                return new TopicContainer(element, element);

            foreach (var item in element.EnumerateArray())
            {
                var nested = FindTopicContainer(item);
                if (nested is not null)
                    return nested;
            }

            return null;
        }

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

    private static bool IsTopicArray(JsonElement element)
    {
        if (element.GetArrayLength() == 0)
            return true;

        var first = element[0];
        if (first.ValueKind != JsonValueKind.Object)
            return false;

        var properties = first.EnumerateObject().ToList();
        if (properties.Any(property => TopicCollectionNames.Contains(property.Name)))
            return false;

        return properties.Any(property =>
            property.Name.Equals("code", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Equals("name", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Equals("nameIt", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Equals("name_it", StringComparison.OrdinalIgnoreCase));
    }

    private static List<CandidateTopic> ParseTopics(JsonElement array)
    {
        var topics = new List<CandidateTopic>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var topicName = item.GetString() ?? string.Empty;
                topics.Add(CreateTopic(null, topicName, null, null, null, []));
                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var name = GetString(item, "name", "label", "topic", "theme");
            var nameIt = GetString(item, "nameIt", "name_it", "nomeIt", "nome_it") ?? name;
            var nameEn = GetString(item, "nameEn", "name_en") ?? name;
            var description = GetString(item, "description", "descrizione");
            var descriptionIt = GetString(
                item,
                "descriptionIt",
                "description_it",
                "descrizioneIt",
                "descrizione_it") ?? description;
            var descriptionEn = GetString(item, "descriptionEn", "description_en") ?? description;
            var examples = GetStringArray(
                item,
                "exampleClippingIds",
                "example_clipping_ids",
                "examples",
                "exampleIds");

            topics.Add(CreateTopic(
                GetString(item, "code", "id", "key"),
                nameIt,
                nameEn,
                descriptionIt,
                descriptionEn,
                examples));
        }

        return topics;
    }

    private static CandidateTopic CreateTopic(
        string? code,
        string? nameIt,
        string? nameEn,
        string? descriptionIt,
        string? descriptionEn,
        List<string> examples)
    {
        nameIt = FirstNotEmpty(nameIt, nameEn, code);
        nameEn = FirstNotEmpty(nameEn, nameIt, code);
        code = string.IsNullOrWhiteSpace(code) ? Slug(nameEn) : code.Trim();

        return new CandidateTopic
        {
            Code = code,
            NameIt = nameIt,
            NameEn = nameEn,
            DescriptionIt = FirstNotEmpty(descriptionIt, descriptionEn, nameIt),
            DescriptionEn = FirstNotEmpty(descriptionEn, descriptionIt, nameEn),
            ExampleClippingIds = examples.Distinct(StringComparer.Ordinal).Take(5).ToList()
        };
    }

    private static string FirstNotEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private static List<string> GetStringArray(JsonElement element, params string[] propertyNames)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!propertyNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase) ||
                property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            return property.Value.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Cast<string>()
                .ToList();
        }

        return [];
    }

    private static string Slug(string value)
    {
        var builder = new StringBuilder();
        var previousWasSeparator = false;
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('_');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().TrimEnd('_');
    }

    private static string? GetString(JsonElement element, params string[] propertyNames)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in element.EnumerateObject())
        {
            if (propertyNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
    }

    private sealed record TopicContainer(JsonElement Parent, JsonElement Topics);
}
