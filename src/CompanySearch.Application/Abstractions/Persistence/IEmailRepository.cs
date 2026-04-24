using CompanySearch.Domain.Entities;

namespace CompanySearch.Application.Abstractions.Persistence;

public interface IEmailRepository
{
    Task AddAsync(SalesEmail email, CancellationToken cancellationToken);

    Task<SalesEmail?> GetLatestByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken);
}
