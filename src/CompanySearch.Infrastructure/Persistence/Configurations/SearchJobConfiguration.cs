using CompanySearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanySearch.Infrastructure.Persistence.Configurations;

public sealed class SearchJobConfiguration : IEntityTypeConfiguration<SearchJob>
{
    public void Configure(EntityTypeBuilder<SearchJob> builder)
    {
        builder.ToTable("SearchJobs");

        builder.HasKey(searchJob => searchJob.Id);
        builder.Property(searchJob => searchJob.LocationQuery).HasMaxLength(500).IsRequired();
        builder.Property(searchJob => searchJob.Source).HasConversion<string>().HasMaxLength(32);
        builder.Property(searchJob => searchJob.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(searchJob => searchJob.ErrorMessage).HasMaxLength(2000);
        builder.Property(searchJob => searchJob.CreatedAtUtc).HasColumnName("CreatedAt");
        builder.Property(searchJob => searchJob.UpdatedAtUtc).HasColumnName("UpdatedAt");
    }
}
