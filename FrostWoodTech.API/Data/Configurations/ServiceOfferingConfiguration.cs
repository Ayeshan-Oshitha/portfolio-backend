using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class ServiceOfferingConfiguration : IEntityTypeConfiguration<ServiceOffering>
{
    public void Configure(EntityTypeBuilder<ServiceOffering> builder)
    {
        builder.ToTable("services");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.Slug).HasColumnName("slug").IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").IsRequired();
        builder.Property(s => s.ShortDescription).HasColumnName("short_description").IsRequired();
        builder.Property(s => s.Description).HasColumnName("description").IsRequired();
        builder.Property(s => s.IconName).HasColumnName("icon_name");
        builder.Property(s => s.IconObjectKey).HasColumnName("icon_object_key");
        builder.Property(s => s.HeroImageId).HasColumnName("hero_image_id");
        builder.Property(s => s.IsPublished).HasColumnName("is_published");
        builder.Property(s => s.PublishedAt).HasColumnName("published_at");

        builder.HasIndex(s => s.Slug).IsUnique();
    }
}
