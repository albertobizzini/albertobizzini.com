using KindleClippings;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using MudBlazor;
using System.Security.Cryptography;
using System.Text;

namespace AlbertoBizzini.Web.Services;

public class KindleClippingService
{
    private readonly ILogger<KindleClippingService> _logger;
    private readonly IJSRuntime _js;
    private readonly HttpClient _httpClient;

    private ParseResult? _data;

    public KindleClippingService(
        ILogger<KindleClippingService> logger,
        IJSRuntime js,
        HttpClient httpClient)
    {
        _logger = logger;
        _js = js;
        _httpClient = httpClient;
    }

    public async Task<ParseResult> LoadAsync()
    {
        if (_data is not null)
            return _data;

        var content = await _httpClient.GetStringAsync("data/My Clippings.txt");
        _data = Parser.Parse(content);

        return _data;
    }

    public async Task<Clipping?> GetClippingById(string id)
    {
        var data = await LoadAsync();
        return data.Clippings.TryGetValue(id, out var clipping) ? clipping : null;
    }

    public async Task<Clipping?> GetClippingOfTheDayAsync(int delta = 0)
    {
        var data = await LoadAsync();

        var clippings = data.Clippings.Values
            .Where(c => c.Type == ClippingType.Highlight && !string.IsNullOrWhiteSpace(c.Text))
            .ToList();

        if (clippings.Count == 0)
            return null;

        // 1. Calcola la data di riferimento in base al delta
        var targetDate = DateTime.Today.AddDays(delta);
        var dateString = targetDate.ToString("yyyy-MM-dd");

        Clipping? bestClipping = null;
        long highestScore = long.MinValue;

        // 2. Trova il clipping con il punteggio più alto per la data corrente
        foreach (var clipping in clippings)
        {
            // Generiamo un ID univoco e stabile per il clipping
            var clippingId = $"{clipping.Book.Title}_{clipping.StartLocation}_{clipping.Text}";

            // Uniamo la data e il clipping in una chiave unica per quel giorno specifico
            var dayClippingKey = $"{dateString}_{clippingId}";

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

}
