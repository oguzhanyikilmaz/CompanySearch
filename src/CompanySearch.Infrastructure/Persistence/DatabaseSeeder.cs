using CompanySearch.Domain.Entities;
using CompanySearch.Domain.Enums;

namespace CompanySearch.Infrastructure.Persistence;

public sealed class DatabaseSeeder(ApplicationDbContext dbContext)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (dbContext.Businesses.Any())
        {
            return;
        }

        var businesses = new[]
        {
            Business.Create("seed-1", "Northwind Dental", "Kadikoy, Istanbul", 40.9860, 29.0294, BusinessSourceType.ManualImport, "+90 555 111 1111", "hello@northwinddental.test", "https://northwinddental.test"),
            Business.Create("seed-2", "Blue Harbor Law", "Besiktas, Istanbul", 41.0436, 29.0093, BusinessSourceType.ManualImport, "+90 555 222 2222", "office@blueharborlaw.test", "https://blueharborlaw.test"),
            Business.Create("seed-3", "Pulse Fitness Studio", "Sisli, Istanbul", 41.0602, 28.9877, BusinessSourceType.ManualImport, "+90 555 333 3333", null, "https://pulsefitnessstudio.test")
        };

        await dbContext.Businesses.AddRangeAsync(businesses, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
