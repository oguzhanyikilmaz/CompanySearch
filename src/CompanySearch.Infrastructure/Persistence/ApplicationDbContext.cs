using CompanySearch.Application.Abstractions.Persistence;
using CompanySearch.Domain.Common;
using CompanySearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompanySearch.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Business> Businesses => Set<Business>();

    public DbSet<SearchJob> SearchJobs => Set<SearchJob>();

    public DbSet<WebsiteAnalysis> WebsiteAnalyses => Set<WebsiteAnalysis>();

    public DbSet<SalesEmail> Emails => Set<SalesEmail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.Touch(timestamp);
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
