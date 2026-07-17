using AlbertoBizzini.Shared;
using System.Runtime.CompilerServices;

namespace AlbertoBizzini.Services;

public class ContactService : IContactService
{
    private readonly IEmailSender _emailSender;
    
    public ContactService(IEmailSender emailSender)
    {
        _emailSender = emailSender;
    }

    public async Task<ContactResponse> SendAsync(
            ContactRequest request,
            ContactContext context,
            CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var email = new ContactEmail { 
                Email = request.Email,
                Message = request.Message,
                Name = request.Name,
                IpAddress = context.IpAddress ?? "n.a.",
                UserAgent = context.UserAgent ?? "n.a.",
            };

            await _emailSender.SendAsync(email, cancellationToken);

            return new ContactResponse
            {
                Success = true,
                ErrorCode = null
            };
        }
        catch (OperationCanceledException)
        {
            return new ContactResponse
            {
                Success = false,
                ErrorCode = "OPERATION_CANCELED"
            };
        }
        // Sostituisci "SmtpException" con l'eccezione specifica della tua libreria se necessario
        catch (System.Net.Mail.SmtpException ex)
        {
            // Qui puoi loggare l'errore interno: _logger.LogError(ex, "Errore SMTP");
            return new ContactResponse
            {
                Success = false,
                ErrorCode = "EMAIL_SERVER_ERROR"
            };
        }
        catch (Exception ex)
        {
            // Logga sempre le eccezioni generiche: _logger.LogError(ex, "Errore generico");
            return new ContactResponse
            {
                Success = false,
                ErrorCode = "UNKNOWN_ERROR"
            };
        }
    }


}
