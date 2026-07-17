namespace AlbertoBizzini.Shared;

public class ContactRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Message { get; set; } = "";

    public bool ResponsibilityTaken { get; set; }

    public string TurnstileToken { get; set; } = "";
}
