using Microsoft.Extensions.Options;

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

    }
}
