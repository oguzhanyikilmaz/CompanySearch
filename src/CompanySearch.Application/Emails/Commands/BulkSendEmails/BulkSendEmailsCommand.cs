using CompanySearch.Application.Abstractions.Email;
using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Application.Common.Models;
using MediatR;

namespace CompanySearch.Application.Emails.Commands.BulkSendEmails;

public sealed record BulkSendEmailsCommand(IReadOnlyCollection<Guid> BusinessIds) : IRequest<BulkEmailSendResultDto>;

public sealed record BulkEmailSendResultDto(int Requested, int Sent, int Failed, IReadOnlyCollection<string> Errors);

public sealed class BulkSendEmailsCommandHandler(
    IBusinessRepository businessRepository,
    IEmailRepository emailRepository,
    IEmailSenderService emailSenderService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<BulkSendEmailsCommand, BulkEmailSendResultDto>
{
    public async Task<BulkEmailSendResultDto> Handle(BulkSendEmailsCommand request, CancellationToken cancellationToken)
    {
        var businesses = await businessRepository.GetByIdsAsync(request.BusinessIds, includeDetails: false, cancellationToken);
        var sent = 0;
        var failed = 0;
        var errors = new List<string>();

        foreach (var business in businesses)
        {
            var email = await emailRepository.GetLatestByBusinessIdAsync(business.Id, cancellationToken);
            if (email is null)
            {
                failed++;
                errors.Add($"{business.Name}: missing generated email.");
                continue;
            }

            var recipient = email.RecipientEmail ?? business.Email;
            if (string.IsNullOrWhiteSpace(recipient))
            {
                failed++;
                errors.Add($"{business.Name}: missing recipient address.");
                continue;
            }

            var result = await emailSenderService.SendAsync(
                new OutgoingEmail(recipient, email.Subject, email.Body, business.Name),
                cancellationToken);

            if (result.Success)
            {
                email.MarkSent(DateTime.UtcNow);
                sent++;
            }
            else
            {
                email.MarkFailed(result.ErrorMessage ?? "Bulk send failed.", DateTime.UtcNow);
                failed++;
                errors.Add($"{business.Name}: {result.ErrorMessage ?? "unknown error"}");
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new BulkEmailSendResultDto(request.BusinessIds.Count, sent, failed, errors);
    }
}
