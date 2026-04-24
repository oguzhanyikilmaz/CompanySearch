using CompanySearch.Domain.Entities;

namespace CompanySearch.Application.Abstractions.Persistence;

public interface ISearchJobRepository
{
    Task AddAsync(SearchJob searchJob, CancellationToken cancellationToken);

    Task<SearchJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
