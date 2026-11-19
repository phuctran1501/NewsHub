namespace NewsHub.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;

public class CompanyRequestConfiguration : IEntityTypeConfiguration<CompanyRequest>
{
    public void Configure(EntityTypeBuilder<CompanyRequest> builder)
    {
        builder.ToTable("company_requests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.CompanyId)
            .HasColumnName("company_id")
            .IsRequired();

        builder.Property(r => r.JournalistId)
            .HasColumnName("journalist_id")
            .IsRequired();

        builder.Property(r => r.Type)
            .HasColumnName("type")
            .HasMaxLength(32)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .HasConversion<string>()
            .HasDefaultValue(CompanyRequestStatus.Pending)
            .HasSentinel((CompanyRequestStatus)0)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(r => r.RespondedAt)
            .HasColumnName("responded_at");

        // Indexes
        builder.HasIndex(r => new { r.CompanyId, r.Status });
        builder.HasIndex(r => new { r.JournalistId, r.Status });

        // Filtered unique index: Ngăn chặn tuyệt đối việc tạo nhiều đơn trùng lặp cùng loại đang chờ xử lý giữa công ty và nhà báo
        builder.HasIndex(r => new { r.CompanyId, r.JournalistId, r.Type })
            .HasFilter("status = 'Pending'")
            .IsUnique();

        // Relationships
        builder.HasOne(r => r.Company)
            .WithMany(c => c.Requests)
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Journalist)
            .WithMany(u => u.CompanyRequests)
            .HasForeignKey(r => r.JournalistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
