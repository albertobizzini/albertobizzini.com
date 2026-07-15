using KindleClippings;

namespace AlbertoBizzini.Web.Services;

public class KindleClippingService
{
    private readonly ILogger<KindleClippingService> _logger;
    private readonly HttpClient _httpClient;

    private Task<ParseResult>? _dataTask;

    public KindleClippingService(
        ILogger<KindleClippingService> logger,
        HttpClient httpClient)
    {
        _logger = logger;
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

        var today = DateOnly.FromDateTime(DateTime.Today);

        var hash = today.GetHashCode() + delta;

        var index = Math.Abs(hash) % clippings.Count;

        return clippings[index];
    }
}