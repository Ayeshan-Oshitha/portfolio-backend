using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Portfolio.API.Entities;

namespace Portfolio.API.Data.Configurations;

public class ProjectImageConfiguration : IEntityTypeConfiguration<ProjectImage>
{
    public void Configure(EntityTypeBuilder<ProjectImage> builder)
    {
        builder.ToTable("project_images");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.ProjectId).HasColumnName("project_id");
        builder.Property(i => i.CloudinaryId).HasColumnName("cloudinary_id").IsRequired();
        builder.Property(i => i.Url).HasColumnName("url").IsRequired();
        builder.Property(i => i.AltText).HasColumnName("alt_text").IsRequired();
        builder.Property(i => i.Width).HasColumnName("width");
        builder.Property(i => i.Height).HasColumnName("height");
        builder.Property(i => i.IsPrimary).HasColumnName("is_primary");
        builder.Property(i => i.SortOrder).HasColumnName("sort_order");

        builder.HasOne(i => i.Project)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Exactly one primary image per project.
        builder.HasIndex(i => i.ProjectId)
            .IsUnique()
            .HasFilter("is_primary");
    }
}
