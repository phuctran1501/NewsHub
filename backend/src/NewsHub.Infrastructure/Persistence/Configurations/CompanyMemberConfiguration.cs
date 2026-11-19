namespace NewsHub.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;

public class CompanyMemberConfiguration : IEntityTypeConfiguration<CompanyMember>
{
    public void Configure(EntityTypeBuilder<CompanyMember> builder)
    {
        builder.ToTable("company_members");

        builder.HasKey(m => new { m.CompanyId, m.UserId });

        builder.Property(m => m.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(m => m.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasMaxLength(32)
            .HasConversion<string>()
            .HasDefaultValue(CompanyMemberRole.Member)
            .HasSentinel((CompanyMemberRole)0)
            .IsRequired();

        builder.Property(m => m.JoinedAt)
            .HasColumnName("joined_at")
            .IsRequired();

        builder.HasIndex(m => m.UserId);

        // Filtered unique index: Mỗi công ty chỉ được phép có duy nhất 1 bản ghi mang vai trò Owner
        builder.HasIndex(m => m.CompanyId)
            .HasFilter("role = 'Owner'")
            .IsUnique();

        // Relationships
        builder.HasOne(m => m.Company)
            .WithMany(c => c.Members)
            .HasForeignKey(m => m.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany(u => u.CompanyMemberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
