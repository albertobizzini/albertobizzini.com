using AlbertoBizzini.Shared;

namespace AlbertoBizzini.Services;

public interface IContactService
{
    Task<ContactResponse> SendAsync(
        ContactRequest request,
        ContactContext context,
        CancellationToken cancellationToken);
}

public class ContactContext
{
    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }
}