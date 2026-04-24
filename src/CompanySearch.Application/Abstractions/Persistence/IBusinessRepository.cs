using CompanySearch.Application.Common.Models;
using CompanySearch.Domain.Entities;
using CompanySearch.Domain.Enums;

namespace CompanySearch.Application.Abstractions.Persistence;

public interface IBusinessRepository
{
    Task AddAsync(Business business, CancellationToken cancellationToken);

    Task<Business?> GetByIdAsync(Guid id, bool includeDetails, CancellationToken cancellationToken);

    Task<Dictionary<string, Business>> GetByExternalIdsAsync(
        BusinessSourceType source,
        IReadOnlyCollection<string> externalIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Business>> GetByIdsAsync(
        IReadOnlyCollection<Guid> businessIds,
        bool includeDetails,
        CancellationToken cancellationToken);

    Task<PagedResult<Business>> ListAsync(BusinessSearchFilters filters, CancellationToken cancellationToken);
}
