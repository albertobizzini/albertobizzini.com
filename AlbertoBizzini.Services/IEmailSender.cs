namespace AlbertoBizzini.Services;

public interface IEmailSender
{
    Task SendAsync(
        ContactEmail email,
        CancellationToken cancellationToken);
}

public class ContactEmail
{
    public string Name { get; init; } = "";

    public string Email { get; init; } = "";

    public string Message { get; init; } = "";

    public string IpAddress { get; init; } = "";

    public string UserAgent { get; init; } = "";
}