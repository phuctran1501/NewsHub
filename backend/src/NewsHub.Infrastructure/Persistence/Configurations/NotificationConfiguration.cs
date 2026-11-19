namespace NewsHub.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsHub.Domain.Entities;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id)
            .HasColumnName("id");

        builder.Property(n => n.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(n => n.Type)
            .HasColumnName("type")
            .HasMaxLength(64)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(n => n.Title)
            .HasColumnName("title")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(n => n.Message)
            .HasColumnName("message")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(n => n.RelatedEntityId)
            .HasColumnName("related_entity_id");

        builder.Property(n => n.TargetUrl)
            .HasColumnName("target_url")
            .HasMaxLength(512);

        builder.Property(n => n.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(n => n.ReadAt)
            .HasColumnName("read_at");

        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Composite index cho feed phân trang sắp xếp theo thời gian tạo mới nhất
        builder.HasIndex(n => new { n.UserId, n.CreatedAt })
            .HasDatabaseName("ix_notifications_user_created");

        // Filtered index phục vụ đếm nhanh số lượng chưa đọc (unread count badge)
        builder.HasIndex(n => n.UserId)
            .HasFilter("is_read = FALSE")
            .HasDatabaseName("ix_notifications_user_unread");

        builder.HasIndex(n => n.RelatedEntityId);

        // Khóa ngoại tới bảng users, xóa user sẽ cascade xóa toàn bộ thông báo
        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
