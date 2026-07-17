namespace AlbertoBizzini.Shared;

public class ContactRequest
{
    public string Name { get; init; } = "";

    public string Email { get; init; } = "";

    public string Message { get; init; } = "";

    public bool ResponsibilityTaken { get; init; }

    public string TurnstileToken { get; init; } = "";

    // Honeypot
    public string? Website { get; init; }
}

public class ContactResponse
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }
}
