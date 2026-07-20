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
    private readonly ISnackbar _snackbar;
    private readonly HttpClient _httpClient;

    private Task<ParseResult>? _dataTask;

    public KindleClippingService(
        ILogger<KindleClippingService> logger,
        IJSRuntime js,
        ISnackbar snackbar,
        HttpClient httpClient)
    {
        _logger = logger;
        _js = js;
        _snackbar = snackbar;
        _httpClient = httpClient;
    }

    public Task<ParseResult> LoadAsync()
    {
        return _dataTask ??= LoadInternalAsync();
    }

    private async Task<ParseResult> LoadInternalAsync()
    {
        var content = await _httpClient.GetStringAsync("data/My Clippings.txt");
        var data = Parser.Parse(content);
        return data;
    }

    public async Task<Clipping?> GetClippingOfTheDay(int delta = 0)
    {
        var data = await LoadAsync();

        var clippings = data.Clippings
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

    public async Task CopyAsync(Clipping clipping, string copiedFeedbackMessage)
    {
        if (string.IsNullOrWhiteSpace(clipping?.Text))
            return;

        var quote = clipping.QuotedText!.ToString();

        StringBuilder suffixBuilder = new StringBuilder();
        string delim = string.Empty;
        if (!string.IsNullOrWhiteSpace(clipping.Book.Title))
        {
            suffixBuilder.Append($"{delim}\"{clipping.Book.Title}\"");
            delim = " - ";
        }
        if (!string.IsNullOrWhiteSpace(clipping.Book.Author))
        {
            suffixBuilder.Append($"{delim}{clipping.Book.Author}");
            delim = " - ";
        }

        var suffix = suffixBuilder.ToString();
        if (!string.IsNullOrWhiteSpace(suffix))
            quote += $" ({suffix})";

        await _js.InvokeVoidAsync("copyToClipboard", quote);
        _snackbar.Add(copiedFeedbackMessage, Severity.Success);
    }
}
