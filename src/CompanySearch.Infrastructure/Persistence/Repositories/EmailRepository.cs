using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanySearch.Infrastructure.Persistence.Repositories;

public sealed class EmailRepository(ApplicationDbContext dbContext) : IEmailRepository
{
    public Task AddAsync(SalesEmail email, CancellationToken cancellationToken)
    {
        return dbContext.Emails.AddAsync(email, cancellationToken).AsTask();
    }

    public Task<SalesEmail?> GetLatestByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken)
    {
        return dbContext.Emails
            .Where(email => email.BusinessId == businessId)
            .OrderByDescending(email => email.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
