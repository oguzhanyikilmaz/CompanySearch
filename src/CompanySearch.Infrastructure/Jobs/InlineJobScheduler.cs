using CompanySearch.Application.Abstractions.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CompanySearch.Infrastructure.Jobs;

public sealed class InlineJobScheduler(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<InlineJobScheduler> logger)
    : IJobScheduler
{
    public string EnqueueBusinessImport(Guid searchJobId)
    {
        Schedule(async dispatcher => await dispatcher.ImportBusinesses(searchJobId));
        return $"inline-import-{searchJobId:N}";
    }

    public string EnqueueWebsiteAnalysis(Guid businessId, bool generateEmailAfterAnalysis)
    {
        Schedule(async dispatcher => await dispatcher.AnalyzeBusiness(businessId, generateEmailAfterAnalysis));
        return $"inline-analysis-{businessId:N}";
    }

    public string EnqueueEmailGeneration(Guid businessId)
    {
        Schedule(async dispatcher => await dispatcher.GenerateEmail(businessId));
        return $"inline-email-generate-{businessId:N}";
    }

    public string EnqueueEmailSending(Guid businessId)
    {
        Schedule(async dispatcher => await dispatcher.SendEmail(businessId));
        return $"inline-email-send-{businessId:N}";
    }

    public string EnqueueBulkEmailSending(IReadOnlyCollection<Guid> businessIds)
    {
        Schedule(async dispatcher => await dispatcher.BulkSendEmails(businessIds));
        return $"inline-bulk-send-{DateTime.UtcNow.Ticks}";
    }

    private void Schedule(Func<BackgroundJobDispatcher, Task> action)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = serviceScopeFactory.CreateAsyncScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<BackgroundJobDispatcher>();
                await action(dispatcher);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Inline background execution failed.");
            }
        });
    }
}
