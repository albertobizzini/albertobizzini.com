using KindleClippings;

namespace AlbertoBizzini.Web.Services;

public class KindleClippingService
{
    private readonly ILogger<KindleClippingService> _logger;
    private readonly HttpClient _httpClient;

    private ParseResult? _data = null;

    public KindleClippingService(
        ILogger<KindleClippingService> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<ParseResult> LoadAsync()
    {
        if (_data is not null)
            return _data;

        _logger.LogInformation("BaseAddress: {addr}", _httpClient.BaseAddress);

        var content = await _httpClient.GetStringAsync("data/My Clippings.txt");
        _data = Parser.Parse(content);
        return _data;
    }

    public async Task<Clipping?> GetClippingOfTheDay()
    {
        var data = await LoadAsync();

        var clippings = data.Clippings
            .Where(c => c.Type == ClippingType.Highlight && !string.IsNullOrWhiteSpace(c.Text))
            .ToList();

        if (clippings.Count == 0)
            return null;

        var today = DateOnly.FromDateTime(DateTime.Today);

        var hash = today.GetHashCode();

        var index = Math.Abs(hash) % clippings.Count;

        return clippings[index];
    }
}