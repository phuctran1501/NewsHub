namespace NewsHub.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Code)
            .HasColumnName("code")
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(c => c.TaxCode)
            .HasColumnName("tax_code")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .HasDefaultValue(CompanyStatus.Pending)
            .HasSentinel((CompanyStatus)0)
            .IsRequired();

        builder.Property(c => c.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(c => c.Version)
            .IsRowVersion();

        // Indexes
        builder.HasIndex(c => c.Code)
            .IsUnique();

        builder.HasIndex(c => c.TaxCode)
            .IsUnique();

        builder.HasIndex(c => c.Status);

        // Relationships
        builder.HasOne(c => c.Owner)
            .WithMany(u => u.OwnedCompanies)
            .HasForeignKey(c => c.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
