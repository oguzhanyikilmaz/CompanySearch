namespace CompanySearch.Application.Abstractions.Jobs;

public interface IJobScheduler
{
    string EnqueueBusinessImport(Guid searchJobId);

    string EnqueueWebsiteAnalysis(Guid businessId, bool generateEmailAfterAnalysis);

    string EnqueueEmailGeneration(Guid businessId);

    string EnqueueEmailSending(Guid businessId);

    string EnqueueBulkEmailSending(IReadOnlyCollection<Guid> businessIds);
}
