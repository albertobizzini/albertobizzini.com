using Microsoft.JSInterop;
using SqliteWasmBlazor;

namespace AlbertoBizzini.Web.Services;

public class KindleClippingDatabase
{
    private const string DatabaseName = "clippings.db";
    private const string DatabaseUrl = "data/clippings.db";
    private const string VersionUrl = "data/clippings.version";
    private const string LocalVersionKey = "kindle-clippings-db-version";

    private readonly ISqliteWasmDatabaseService _databaseService;
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _js;
    private readonly ILogger<KindleClippingDatabase> _logger;

    public KindleClippingDatabase(
        ISqliteWasmDatabaseService databaseService,
        HttpClient httpClient,
        IJSRuntime js,
        ILogger<KindleClippingDatabase> logger)
    {
        _databaseService = databaseService;
        _httpClient = httpClient;
        _js = js;
        _logger = logger;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        var remoteVersion = (
            await _httpClient.GetStringAsync(
                VersionUrl,
                cancellationToken)).Trim();

        var localVersion = await GetLocalVersionAsync();

        var databaseExists =
            await _databaseService.ExistsDatabaseAsync(
                DatabaseName,
                cancellationToken);

        if (databaseExists &&
            string.Equals(
                localVersion,
                remoteVersion,
                StringComparison.Ordinal))
        {
            _logger.LogDebug(
                "Clipping database is up to date ({Version}).",
                remoteVersion);

            return;
        }

        _logger.LogInformation(
            "Updating clipping database: {LocalVersion} → {RemoteVersion}",
            localVersion ?? "(none)",
            remoteVersion);

        var data = await _httpClient.GetByteArrayAsync(
            DatabaseUrl,
            cancellationToken);

        await _databaseService.ImportDatabaseAsync(
            DatabaseName,
            data,
            cancellationToken);

        await SetLocalVersionAsync(remoteVersion);

        _logger.LogInformation(
            "Clipping database updated successfully.");
    }

    private async Task<string?> GetLocalVersionAsync()
    {
        return await _js.InvokeAsync<string?>(
            "localStorage.getItem",
            LocalVersionKey);
    }

    private async Task SetLocalVersionAsync(string version)
    {
        await _js.InvokeVoidAsync(
            "localStorage.setItem",
            LocalVersionKey,
            version);
    }
}