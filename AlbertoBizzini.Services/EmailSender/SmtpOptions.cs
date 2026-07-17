namespace AlbertoBizzini.Services;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = "";

    public int Port { get; init; }

    public string Username { get; init; } = "";

    public string Password { get; init; } = "";

    public string From { get; init; } = "";

    public string To { get; init; } = "";
}