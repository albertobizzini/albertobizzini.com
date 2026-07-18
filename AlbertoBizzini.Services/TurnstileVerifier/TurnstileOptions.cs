namespace AlbertoBizzini.Services;

public sealed class TurnstileOptions
{
    public const string SectionName = "Turnstile";
    public string Endpoint { get; set; } = 
        "https://challenges.cloudflare.com/turnstile/v0/siteverify";
    public string SecretKey { get; init; } = "";
}