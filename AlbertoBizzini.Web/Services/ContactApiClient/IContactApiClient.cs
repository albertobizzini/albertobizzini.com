using AlbertoBizzini.Shared;
namespace AlbertoBizzini.Web.Services;

public interface IContactApiClient
{
    Task<ContactResponse> SendAsync(
        ContactRequest request,
        CancellationToken cancellationToken = default);
}