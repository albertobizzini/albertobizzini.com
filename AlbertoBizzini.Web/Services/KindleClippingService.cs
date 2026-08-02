using KindleClippings;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AlbertoBizzini.Web.Services;

public class KindleClippingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<KindleClippingService> _logger;
    private readonly HttpClient _httpClient;

    private List<Clipping>? _data;
    private Dictionary<string, Clipping>? _dict;



    public KindleClippingService(
        ILogger<KindleClippingService> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<List<Clipping>> LoadAsync()
    {
        if (_data is not null)
            return _data;

        var json = await _httpClient.GetStringAsync(
            "data/clippings.json");

        _data = JsonSerializer.Deserialize<List<Clipping>>(
            json,
            JsonOptions) ?? [];

        _logger.LogInformation(
            "KindleClippingService.LoadAsync: loaded {count} clippings",
            _data.Count);

        _dict = new();

        foreach (var clipping in _data)
            _dict.Add(clipping.Id, clipping);

        return _data;
    }

    public async Task<List<Clipping>> GetAllClippingsAsync()
    {
        var data = await LoadAsync();
        return data;
    }

    public async Task<Clipping?> GetClippingByIdAsync(string id)
    {
        var data = await LoadAsync();
        return _dict.TryGetValue(id, out var clipping) ? clipping : null;
    }

    public async Task<Clipping?> GetClippingOfTheDayAsync(int delta = 0)
    {
        var data = await LoadAsync();

        if (data.Count == 0)
            return null;

        // 1. Calcola la data di riferimento in base al delta
        var targetDate = DateTime.Today.AddDays(delta);
        var dateString = targetDate.ToString("yyyy-MM-dd");

        Clipping? bestClipping = null;
        long highestScore = long.MinValue;

        // 2. Trova il clipping con il punteggio più alto per la data corrente
        foreach (var clipping in data)
        {
            // Uniamo la data e il clipping in una chiave unica per quel giorno specifico
            var dayClippingKey = $"{dateString}_{clipping.Id}";

            // Calcoliamo un punteggio numerico deterministico per questa combinazione
            long score = GetStableHash64(dayClippingKey);

            // Il clipping con il punteggio massimo (o minimo) vince
            if (score > highestScore)
            {
                highestScore = score;
                bestClipping = clipping;
            }
        }

        return bestClipping;
    }

    // Algoritmo di hashing a 64-bit stabile basato su SHA256
    private static long GetStableHash64(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToInt64(bytes, 0);
    }


    public static string BrowseAuthorClippingsHref(string clippingId, bool toTitle)
    {
        var parameter = toTitle ? "Title" : "Author";

        return $"/clippings?{parameter}={Uri.EscapeDataString(clippingId)}";
    }

}
