namespace AlbertoBizzini.Services;

public sealed class TurnstileOptions
{
    public const string SectionName = "Turnstile";

    public string SecretKey { get; init; } = "";
}