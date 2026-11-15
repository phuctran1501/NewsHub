namespace NewsHub.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NewsHub.Domain.Entities;

public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("media_files");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id");

        builder.Property(m => m.PublicId)
            .HasColumnName("public_id")
            .HasMaxLength(256)
            .IsRequired();

        // Unique index cho public_id để đảm bảo định danh Cloudinary duy nhất và tra cứu nhanh
        builder.HasIndex(m => m.PublicId)
            .IsUnique();

        builder.Property(m => m.Url)
            .HasColumnName("url")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(m => m.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(m => m.Format)
            .HasColumnName("format")
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(m => m.Bytes)
            .HasColumnName("bytes")
            .IsRequired();

        builder.Property(m => m.Width)
            .HasColumnName("width")
            .IsRequired();

        builder.Property(m => m.Height)
            .HasColumnName("height")
            .IsRequired();

        // Lưu enum Folder dưới dạng string trong DB để tương thích Supabase Table Editor
        builder.Property(m => m.Folder)
            .HasConversion<string>()
            .HasColumnName("folder")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.UploadedByUserId)
            .HasColumnName("uploaded_by_user_id")
            .IsRequired();

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Khóa ngoại liên kết tới bảng users
        builder.HasOne(m => m.UploadedByUser)
            .WithMany(u => u.MediaFiles)
            .HasForeignKey(m => m.UploadedByUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
