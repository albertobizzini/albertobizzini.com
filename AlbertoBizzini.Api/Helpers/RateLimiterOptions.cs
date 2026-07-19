namespace AlbertoBizzini.Api;

public sealed class RateLimiterOptions
{
    public const string SectionName = "RateLimiter";

    public int PermitLimit { get; init; } = 3;

    public int WindowMinutes { get; init; } = 15;

    public int QueueLimit { get; init; } = 0;

    public bool AutoReplenishment { get; init; } = true;
}