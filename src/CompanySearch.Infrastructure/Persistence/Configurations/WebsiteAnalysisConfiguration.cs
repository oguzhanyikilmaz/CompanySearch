using CompanySearch.Domain.Entities;
using CompanySearch.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanySearch.Infrastructure.Persistence.Configurations;

public sealed class WebsiteAnalysisConfiguration : IEntityTypeConfiguration<WebsiteAnalysis>
{
    public void Configure(EntityTypeBuilder<WebsiteAnalysis> builder)
    {
        builder.ToTable("WebsiteAnalysis");

        builder.HasKey(analysis => analysis.Id);
        builder.Property(analysis => analysis.Summary).HasMaxLength(2000);
        builder.Property(analysis => analysis.CreatedAtUtc).HasColumnName("CreatedAt");
        builder.Property(analysis => analysis.UpdatedAtUtc).HasColumnName("UpdatedAt");

        var snapshotConverter = JsonValueConverters.CreateConverter<WebsiteCrawlSnapshot>();
        var snapshotComparer = JsonValueConverters.CreateComparer<WebsiteCrawlSnapshot>();
        builder.Property(analysis => analysis.Snapshot)
            .HasConversion(snapshotConverter)
            .Metadata.SetValueComparer(snapshotComparer);
        builder.Property(analysis => analysis.Snapshot).HasColumnType("jsonb");

        var issuesConverter = JsonValueConverters.CreateConverter<List<WebsiteIssue>>();
        var issuesComparer = JsonValueConverters.CreateComparer<List<WebsiteIssue>>();
        builder.Property(analysis => analysis.Issues)
            .HasConversion(issuesConverter)
            .Metadata.SetValueComparer(issuesComparer);
        builder.Property(analysis => analysis.Issues).HasColumnType("jsonb");
    }
}
