namespace NewsHub.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsHub.Domain.Entities;

public class CompanyFollowerConfiguration : IEntityTypeConfiguration<CompanyFollower>
{
    public void Configure(EntityTypeBuilder<CompanyFollower> builder)
    {
        builder.ToTable("company_followers");

        builder.HasKey(f => new { f.CompanyId, f.UserId });

        builder.Property(f => f.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(f => f.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(f => f.UserId);

        // Relationships
        builder.HasOne(f => f.Company)
            .WithMany(c => c.Followers)
            .HasForeignKey(f => f.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(f => f.User)
            .WithMany(u => u.FollowedCompanies)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
