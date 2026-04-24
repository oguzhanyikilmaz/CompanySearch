using CompanySearch.Application.Abstractions.Email;
using CompanySearch.Application.Common.Models;
using CompanySearch.Infrastructure.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CompanySearch.Infrastructure.Email;

public sealed class SmtpEmailSenderService(
    IOptions<EmailDeliveryOptions> options,
    ILogger<SmtpEmailSenderService> logger)
    : IEmailSenderService
{
    private readonly EmailDeliveryOptions _options = options.Value;

    public async Task<EmailSendResult> SendAsync(OutgoingEmail email, CancellationToken cancellationToken)
    {
        if (_options.SandboxMode || string.IsNullOrWhiteSpace(_options.Host))
        {
            logger.LogInformation("Sandbox email delivery for {Recipient}: {Subject}", email.RecipientEmail, email.Subject);
            return new EmailSendResult(true, $"sandbox-{Guid.NewGuid():N}", null);
        }

        for (var attempt = 1; attempt <= Math.Max(1, _options.RetryCount); attempt++)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
                message.To.Add(MailboxAddress.Parse(email.RecipientEmail));
                message.Subject = email.Subject;
                message.Body = new TextPart("plain")
                {
                    Text = email.Body
                };

                using var client = new SmtpClient();
                var socketOptions = _options.UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto;

                await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);

                if (!string.IsNullOrWhiteSpace(_options.Username))
                {
                    await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
                }

                var response = await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                logger.LogInformation("Email delivered to {Recipient}", email.RecipientEmail);
                return new EmailSendResult(true, response, null);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Email delivery attempt {Attempt} failed for {Recipient}", attempt, email.RecipientEmail);

                if (attempt == _options.RetryCount)
                {
                    return new EmailSendResult(false, null, exception.Message);
                }

                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken);
            }
        }

        return new EmailSendResult(false, null, "Unexpected SMTP failure.");
    }
}
