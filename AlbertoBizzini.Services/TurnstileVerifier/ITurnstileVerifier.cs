using System.Text.Json.Serialization;

namespace AlbertoBizzini.Services;

public interface ITurnstileVerifier
{
    Task<bool> VerifyAsync(
        string token,
        string? remoteIp,
        CancellationToken cancellationToken);
}

public sealed class TurnstileVerificationResult
{
    public bool Success { get; init; }

    public IReadOnlyList<string> ErrorCodes { get; init; } = [];
}

internal sealed class TurnstileVerifyResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("error-codes")]
    public string[] ErrorCodes { get; init; } = [];
}