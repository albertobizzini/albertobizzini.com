using System.Text.Json.Nodes;

namespace KindleClippings.ConsoleApp.Ai;

internal static class OllamaJsonSchemas
{
    public static JsonObject TopicSet(int minimumTopics, int maximumTopics) =>
        new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["topics"] = TopicArray(minimumTopics, maximumTopics)
            },
            ["required"] = new JsonArray("topics"),
            ["additionalProperties"] = false
        };

    public static JsonObject TaxonomyVariant(int minimumTopics, int maximumTopics) =>
        new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["name"] = new JsonObject { ["type"] = "string" },
                ["description"] = new JsonObject { ["type"] = "string" },
                ["topics"] = TopicArray(minimumTopics, maximumTopics)
            },
            ["required"] = new JsonArray("name", "description", "topics"),
            ["additionalProperties"] = false
        };

    public static JsonObject Classification(IEnumerable<string> topicCodes)
    {
        var codeValues = new JsonArray();
        foreach (var code in topicCodes.OrderBy(x => x, StringComparer.Ordinal))
            codeValues.Add(code);

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["primaryTopic"] = new JsonObject
                {
                    ["anyOf"] = new JsonArray(
                        TopicAssignment(codeValues.DeepClone().AsArray()),
                        new JsonObject { ["type"] = "null" })
                },
                ["secondaryTopics"] = new JsonObject
                {
                    ["type"] = "array",
                    ["maxItems"] = 2,
                    ["items"] = TopicAssignment(codeValues.DeepClone().AsArray())
                },
                ["aphorismScore"] = Score(),
                ["contextDependencyScore"] = Score()
            },
            ["required"] = new JsonArray(
                "primaryTopic",
                "secondaryTopics",
                "aphorismScore",
                "contextDependencyScore"),
            ["additionalProperties"] = false
        };
    }

    private static JsonObject TopicArray(int minimumTopics, int maximumTopics) =>
        new()
        {
            ["type"] = "array",
            ["minItems"] = minimumTopics,
            ["maxItems"] = maximumTopics,
            ["items"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["code"] = new JsonObject { ["type"] = "string" },
                    ["nameIt"] = new JsonObject { ["type"] = "string" },
                    ["nameEn"] = new JsonObject { ["type"] = "string" },
                    ["descriptionIt"] = new JsonObject { ["type"] = "string" },
                    ["descriptionEn"] = new JsonObject { ["type"] = "string" },
                    ["exampleClippingIds"] = new JsonObject
                    {
                        ["type"] = "array",
                        ["maxItems"] = 5,
                        ["items"] = new JsonObject { ["type"] = "string" }
                    }
                },
                ["required"] = new JsonArray(
                    "code",
                    "nameIt",
                    "nameEn",
                    "descriptionIt",
                    "descriptionEn",
                    "exampleClippingIds"),
                ["additionalProperties"] = false
            }
        };

    private static JsonObject TopicAssignment(JsonArray topicCodes) =>
        new()
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["code"] = new JsonObject
                {
                    ["type"] = "string",
                    ["enum"] = topicCodes
                },
                ["score"] = Score()
            },
            ["required"] = new JsonArray("code", "score"),
            ["additionalProperties"] = false
        };

    private static JsonObject Score() =>
        new()
        {
            ["type"] = "number",
            ["minimum"] = 0,
            ["maximum"] = 1
        };
}
