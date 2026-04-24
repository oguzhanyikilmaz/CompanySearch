using CompanySearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanySearch.Infrastructure.Persistence.Configurations;

public sealed class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("Businesses");

        builder.HasKey(business => business.Id);

        builder.Property(business => business.ExternalId).HasMaxLength(128).IsRequired();
        builder.Property(business => business.Name).HasMaxLength(300).IsRequired();
        builder.Property(business => business.Address).HasMaxLength(500).IsRequired();
        builder.Property(business => business.Phone).HasMaxLength(64);
        builder.Property(business => business.Email).HasMaxLength(256);
        builder.Property(business => business.Website).HasMaxLength(512);
        builder.Property(business => business.Source).HasConversion<string>().HasMaxLength(32);
        builder.Property(business => business.Priority).HasConversion<string>().HasMaxLength(32);
        builder.Property(business => business.CreatedAtUtc).HasColumnName("CreatedAt");
        builder.Property(business => business.UpdatedAtUtc).HasColumnName("UpdatedAt");

        var tagsConverter = JsonValueConverters.CreateConverter<List<string>>();
        var tagsComparer = JsonValueConverters.CreateComparer<List<string>>();

        builder.Property(business => business.Tags)
            .HasConversion(tagsConverter)
            .Metadata.SetValueComparer(tagsComparer);
        builder.Property(business => business.Tags).HasColumnType("jsonb");

        builder.HasIndex(business => new { business.Source, business.ExternalId }).IsUnique();

        builder.HasMany(business => business.Analyses)
            .WithOne(analysis => analysis.Business)
            .HasForeignKey(analysis => analysis.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(business => business.Emails)
            .WithOne(email => email.Business)
            .HasForeignKey(email => email.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
