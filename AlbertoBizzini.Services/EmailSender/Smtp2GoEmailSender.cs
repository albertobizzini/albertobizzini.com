using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;


namespace AlbertoBizzini.Services;

public class Smtp2GoEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;

    public Smtp2GoEmailSender(IOptions<SmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(
        ContactEmail email,
        CancellationToken cancellationToken)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(
            _options.FromDisplayName,
            _options.From));

        message.To.Add(new MailboxAddress(
            _options.ToDisplayName,
            _options.To));

        // Permette di rispondere direttamente al mittente
        message.ReplyTo.Add(new MailboxAddress(
            email.Name,
            email.Email));

        message.Subject = "Messaggio dal sito albertobizzini.com";

        message.Body = new TextPart("plain")
        {
            Text =
$"""
È stato ricevuto un nuovo messaggio dal sito.

Nome:
{email.Name}

Email:
{email.Email}

Messaggio:
{email.Message}

----------------------------------------

IP:
{email.IpAddress}

User Agent:
{email.UserAgent}

Ricevuto:
{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
"""
        };

        using var smtp = new SmtpClient();

        smtp.Timeout = 10_000;

        await smtp.ConnectAsync(
            _options.Host,
            _options.Port,
            SecureSocketOptions.StartTls,
            cancellationToken);

        await smtp.AuthenticateAsync(
            _options.Username,
            _options.Password,
            cancellationToken);

        await smtp.SendAsync(
            message,
            cancellationToken);

        await smtp.DisconnectAsync(
            true,
            cancellationToken);
    }
}
