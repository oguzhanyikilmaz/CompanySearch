using CompanySearch.Application.Common.Models;

namespace CompanySearch.Application.Abstractions.Email;

public interface IEmailSenderService
{
    Task<EmailSendResult> SendAsync(OutgoingEmail email, CancellationToken cancellationToken);
}
