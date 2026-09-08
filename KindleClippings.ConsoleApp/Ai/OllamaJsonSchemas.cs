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
}
