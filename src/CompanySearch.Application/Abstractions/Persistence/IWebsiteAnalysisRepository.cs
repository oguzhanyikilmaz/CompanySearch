using CompanySearch.Domain.Entities;

namespace CompanySearch.Application.Abstractions.Persistence;

public interface IWebsiteAnalysisRepository
{
    Task AddAsync(WebsiteAnalysis analysis, CancellationToken cancellationToken);

    Task<WebsiteAnalysis?> GetLatestByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken);
}
