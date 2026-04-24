using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanySearch.Infrastructure.Persistence.Repositories;

public sealed class WebsiteAnalysisRepository(ApplicationDbContext dbContext) : IWebsiteAnalysisRepository
{
    public Task AddAsync(WebsiteAnalysis analysis, CancellationToken cancellationToken)
    {
        return dbContext.WebsiteAnalyses.AddAsync(analysis, cancellationToken).AsTask();
    }

    public Task<WebsiteAnalysis?> GetLatestByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken)
    {
        return dbContext.WebsiteAnalyses
            .Where(analysis => analysis.BusinessId == businessId)
            .OrderByDescending(analysis => analysis.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
