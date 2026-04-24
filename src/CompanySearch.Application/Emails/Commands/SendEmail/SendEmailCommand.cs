using CompanySearch.Application.Abstractions.Email;
using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Application.Businesses.Queries.GetBusinessById;
using CompanySearch.Application.Common.Exceptions;
using CompanySearch.Application.Common.Models;
using MediatR;

namespace CompanySearch.Application.Emails.Commands.SendEmail;

public sealed record SendEmailCommand(Guid BusinessId) : IRequest<SalesEmailDto>;

public sealed class SendEmailCommandHandler(
    IBusinessRepository businessRepository,
    IEmailRepository emailRepository,
    IEmailSenderService emailSenderService,
    IUnitOfWork unitOfWork)
    : IRequestHandler<SendEmailCommand, SalesEmailDto>
{
    public async Task<SalesEmailDto> Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        var business = await businessRepository.GetByIdAsync(request.BusinessId, includeDetails: false, cancellationToken)
            ?? throw new NotFoundException($"Business '{request.BusinessId}' was not found.");

        var email = await emailRepository.GetLatestByBusinessIdAsync(business.Id, cancellationToken)
            ?? throw new BusinessRuleException("No generated email exists for this business.");

        var recipient = email.RecipientEmail ?? business.Email;
        if (string.IsNullOrWhiteSpace(recipient))
        {
            throw new BusinessRuleException("No recipient email address is available for this business.");
        }

        var result = await emailSenderService.SendAsync(
            new OutgoingEmail(recipient, email.Subject, email.Body, business.Name),
            cancellationToken);

        if (result.Success)
        {
            email.MarkSent(DateTime.UtcNow);
        }
        else
        {
            email.MarkFailed(result.ErrorMessage ?? "SMTP delivery failed.", DateTime.UtcNow);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GetBusinessByIdQueryHandler.MapEmail(email);
    }
}
