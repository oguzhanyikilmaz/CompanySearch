using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanySearch.Infrastructure.Persistence.Repositories;

public sealed class SearchJobRepository(ApplicationDbContext dbContext) : ISearchJobRepository
{
    public Task AddAsync(SearchJob searchJob, CancellationToken cancellationToken)
    {
        return dbContext.SearchJobs.AddAsync(searchJob, cancellationToken).AsTask();
    }

    public Task<SearchJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.SearchJobs.FirstOrDefaultAsync(searchJob => searchJob.Id == id, cancellationToken);
    }
}
