using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Portfolio.API.Entities;

namespace Portfolio.API.Data.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.Name).HasColumnName("name").IsRequired();
        builder.Property(t => t.Slug).HasColumnName("slug").IsRequired();
        builder.Property(t => t.IsTechnology).HasColumnName("is_technology");
        builder.Property(t => t.TechnologyCategory).HasColumnName("technology_category").HasColumnType("tech_category");
        builder.Property(t => t.IconCloudinaryId).HasColumnName("icon_cloudinary_id");
        builder.Property(t => t.IconUrl).HasColumnName("icon_url");
        builder.Property(t => t.ColorHex).HasColumnName("color_hex");
        builder.Property(t => t.SortOrder).HasColumnName("sort_order");

        builder.HasIndex(t => t.Slug).IsUnique();
        builder.HasIndex(t => t.IsTechnology);
    }
}
