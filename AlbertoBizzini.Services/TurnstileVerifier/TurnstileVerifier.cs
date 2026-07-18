using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;

namespace AlbertoBizzini.Services;

public class TurnstileVerifier : ITurnstileVerifier
{
    private readonly TurnstileOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TurnstileVerifier> _logger;

    public TurnstileVerifier(
        IOptions<TurnstileOptions> options,
        HttpClient httpClient,
        ILogger<TurnstileVerifier> logger
        )
    {
        _options = options.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(
        string token,
        string? remoteIp,
        CancellationToken cancellationToken)
    {
        var request = new
        {
            secret = _options.SecretKey,
            response = token,
            remoteip = IPAddress.TryParse(remoteIp, out _) ? remoteIp : null
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                _options.Endpoint,
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "TurnstileVerifier POST failed to {Endpoint} with status code {StatusCode}",
                    _options.Endpoint,
                    response.StatusCode);

                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<TurnstileVerifyResponse>(
                cancellationToken: cancellationToken);

            if (result is null)
            {
                _logger.LogWarning(
                    "TurnstileVerifier returned an empty response");

                return false;
            }

            if (!result.Success)
            {
                _logger.LogWarning(
                    "TurnstileVerifier response NOT OK: {Errors}",
                    string.Join(", ", result.ErrorCodes));

                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "TurnstileVerifier failed");

            return false;
        }
    }
}