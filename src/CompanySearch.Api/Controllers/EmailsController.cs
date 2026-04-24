using CompanySearch.Api.Contracts.Emails;
using CompanySearch.Application.Businesses.Queries.GetBusinessById;
using CompanySearch.Application.Emails.Commands.BulkSendEmails;
using CompanySearch.Application.Emails.Commands.GenerateEmail;
using CompanySearch.Application.Emails.Commands.SendEmail;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CompanySearch.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class EmailsController(ISender sender) : ControllerBase
{
    [HttpPost("generate-email/{businessId:guid}")]
    public Task<SalesEmailDto> GenerateEmail(Guid businessId, CancellationToken cancellationToken)
    {
        return sender.Send(new GenerateEmailCommand(businessId), cancellationToken);
    }

    [HttpPost("send-email/{businessId:guid}")]
    public Task<SalesEmailDto> SendEmail(Guid businessId, CancellationToken cancellationToken)
    {
        return sender.Send(new SendEmailCommand(businessId), cancellationToken);
    }

    [HttpPost("emails/bulk-send")]
    public Task<BulkEmailSendResultDto> BulkSendEmails([FromBody] BulkSendEmailsRequest request, CancellationToken cancellationToken)
    {
        return sender.Send(new BulkSendEmailsCommand(request.BusinessIds), cancellationToken);
    }
}
