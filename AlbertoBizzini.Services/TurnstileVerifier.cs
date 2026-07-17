using Microsoft.Extensions.Options;

namespace AlbertoBizzini.Services;

public class TurnstileVerifier : ITurnstileVerifier
{
    private readonly TurnstileOptions _options;
    private readonly HttpClient _httpClient;

    public TurnstileVerifier(
        IOptions<TurnstileOptions> options,
        HttpClient httpClient
        )
    {
        _options = options.Value;
        _httpClient = httpClient;
    }

    public Task<bool> VerifyAsync(string token, string? remoteIp, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}