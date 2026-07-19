using AlbertoBizzini.Shared;
using System.Net.Http.Json;

namespace AlbertoBizzini.Web.Services;

public sealed class ContactApiClient : IContactApiClient
{
    private readonly HttpClient _httpClient;

    public ContactApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ContactResponse> SendAsync(
        ContactRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "contact",
            request,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<ContactResponse>(cancellationToken)
                   ?? new ContactResponse
                   {
                       Success = false,
                       ErrorCode = "InvalidResponse"
                   };
        }

        var error = await response.Content.ReadFromJsonAsync<ContactResponse>(cancellationToken);

        return error ?? new ContactResponse
        {
            Success = false,
            ErrorCode = $"HTTP{(int)response.StatusCode}"
        };
    }
}