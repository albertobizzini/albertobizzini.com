using AlbertoBizzini.Shared;
using System.Runtime.CompilerServices;

namespace AlbertoBizzini.Services;

public class ContactService : IContactService
{
    private readonly ITurnstileVerifier _turnstileVerifier;
    private readonly IEmailSender _emailSender;

    public ContactService(
        ITurnstileVerifier turnstileVerifier,
        IEmailSender emailSender)
    {
        _emailSender = emailSender;
        _turnstileVerifier = turnstileVerifier;
    }

    public async Task<ContactResponse> SendAsync(
            ContactRequest request,
            ContactContext context,
            CancellationToken cancellationToken)
    {
        var turnstileTokenIsValid = await _turnstileVerifier.VerifyAsync(request.TurnstileToken, context.IpAddress, cancellationToken);
        if (!turnstileTokenIsValid)
        {
            return new ContactResponse
            {
                Success = false,
                ErrorCode = "TURNSTILETOKEN_NOT_VALID"
            };
        }

        return await SendEmail(request, context, cancellationToken);
    }

    private async Task<ContactResponse> SendEmail(ContactRequest request, ContactContext context, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var email = new ContactEmail
            {
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
