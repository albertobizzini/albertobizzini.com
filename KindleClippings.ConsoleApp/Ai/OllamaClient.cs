using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace KindleClippings.ConsoleApp.Ai;

public sealed class OllamaClient : IDisposable
{
    private readonly HttpClient _httpClient;

    public OllamaClient(string baseUrl, TimeSpan timeout)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/"),
            Timeout = timeout
        };
    }

    public async Task<OllamaResult<T>> GenerateJsonAsync<T>(
        string model,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            model,
            stream = false,
            think = false,
            format = "json",
            options = new
            {
                temperature = 0.1,
                seed = 42
            },
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var stopwatch = Stopwatch.StartNew();
        using var response = await _httpClient.PostAsJsonAsync(
            "api/chat",
            request,
            JsonDefaults.Options,
            cancellationToken);

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        var envelope = JsonSerializer.Deserialize<OllamaChatResponse>(
            responseText,
            JsonDefaults.Options)
            ?? throw new InvalidOperationException("Ollama ha restituito una risposta vuota.");

        var value = JsonSerializer.Deserialize<T>(
            envelope.Message.Content,
            JsonDefaults.Options)
            ?? throw new InvalidOperationException("Ollama ha restituito JSON vuoto.");

        return new OllamaResult<T>(value, stopwatch.ElapsedMilliseconds);
    }

    public void Dispose() => _httpClient.Dispose();

    private sealed class OllamaChatResponse
    {
        public OllamaMessage Message { get; init; } = new();
    }

    private sealed class OllamaMessage
    {
        public string Content { get; init; } = string.Empty;
    }
}

public sealed record OllamaResult<T>(T Value, long DurationMilliseconds);
