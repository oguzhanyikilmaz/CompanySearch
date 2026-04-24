using CompanySearch.Application.Analysis.Commands.AnalyzeBusiness;
using CompanySearch.Application.Businesses.Commands.ImportBusinesses;
using CompanySearch.Application.Emails.Commands.BulkSendEmails;
using CompanySearch.Application.Emails.Commands.GenerateEmail;
using CompanySearch.Application.Emails.Commands.SendEmail;
using MediatR;

namespace CompanySearch.Infrastructure.Jobs;

public sealed class BackgroundJobDispatcher(ISender sender)
{
    public Task ImportBusinesses(Guid searchJobId)
    {
        return sender.Send(new ImportBusinessesCommand(searchJobId));
    }

    public Task AnalyzeBusiness(Guid businessId, bool generateEmailAfterAnalysis)
    {
        return sender.Send(new AnalyzeBusinessCommand(businessId, generateEmailAfterAnalysis));
    }

    public Task GenerateEmail(Guid businessId)
    {
        return sender.Send(new GenerateEmailCommand(businessId));
    }

    public Task SendEmail(Guid businessId)
    {
        return sender.Send(new SendEmailCommand(businessId));
    }

    public Task BulkSendEmails(IReadOnlyCollection<Guid> businessIds)
    {
        return sender.Send(new BulkSendEmailsCommand(businessIds));
    }
}
