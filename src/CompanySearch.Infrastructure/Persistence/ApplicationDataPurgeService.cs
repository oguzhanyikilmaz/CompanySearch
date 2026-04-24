using CompanySearch.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CompanySearch.Infrastructure.Persistence;

public sealed class ApplicationDataPurgeService(ApplicationDbContext dbContext) : IApplicationDataPurge
{
    public async Task PurgeAllAsync(CancellationToken cancellationToken)
    {
        // Önce bağımlı tablolar (FK güvenliği için açık sıra)
        await dbContext.WebsiteAnalyses.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Emails.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Businesses.ExecuteDeleteAsync(cancellationToken);
        await dbContext.SearchJobs.ExecuteDeleteAsync(cancellationToken);
    }
}
