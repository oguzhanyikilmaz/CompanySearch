using CompanySearch.Application.Abstractions.Jobs;
using Hangfire;

namespace CompanySearch.Infrastructure.Jobs;

public sealed class HangfireJobScheduler(IBackgroundJobClient backgroundJobClient) : IJobScheduler
{
    public string EnqueueBusinessImport(Guid searchJobId)
    {
        return backgroundJobClient.Enqueue<BackgroundJobDispatcher>(dispatcher => dispatcher.ImportBusinesses(searchJobId));
    }

    public string EnqueueWebsiteAnalysis(Guid businessId, bool generateEmailAfterAnalysis)
    {
        return backgroundJobClient.Enqueue<BackgroundJobDispatcher>(dispatcher => dispatcher.AnalyzeBusiness(businessId, generateEmailAfterAnalysis));
    }

    public string EnqueueEmailGeneration(Guid businessId)
    {
        return backgroundJobClient.Enqueue<BackgroundJobDispatcher>(dispatcher => dispatcher.GenerateEmail(businessId));
    }

    public string EnqueueEmailSending(Guid businessId)
    {
        return backgroundJobClient.Enqueue<BackgroundJobDispatcher>(dispatcher => dispatcher.SendEmail(businessId));
    }

    public string EnqueueBulkEmailSending(IReadOnlyCollection<Guid> businessIds)
    {
        return backgroundJobClient.Enqueue<BackgroundJobDispatcher>(dispatcher => dispatcher.BulkSendEmails(businessIds));
    }
}
