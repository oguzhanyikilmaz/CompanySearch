using CompanySearch.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompanySearch.Infrastructure.Persistence.Configurations;

public sealed class SalesEmailConfiguration : IEntityTypeConfiguration<SalesEmail>
{
    public void Configure(EntityTypeBuilder<SalesEmail> builder)
    {
        builder.ToTable("Emails");

        builder.HasKey(email => email.Id);
        builder.Property(email => email.Subject).HasMaxLength(300).IsRequired();
        builder.Property(email => email.Body).HasColumnType("text").IsRequired();
        builder.Property(email => email.RecipientEmail).HasMaxLength(256);
        builder.Property(email => email.GeneratedByModel).HasMaxLength(128);
        builder.Property(email => email.SentStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(email => email.LastError).HasMaxLength(2000);
        builder.Property(email => email.CreatedAtUtc).HasColumnName("CreatedAt");
        builder.Property(email => email.UpdatedAtUtc).HasColumnName("UpdatedAt");
    }
}
