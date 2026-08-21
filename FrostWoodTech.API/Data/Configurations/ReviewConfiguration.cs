using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews", t => t.HasCheckConstraint("ck_reviews_rating_range", "rating BETWEEN 1 AND 5"));

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(r => r.Country).HasColumnName("country").HasMaxLength(100).IsRequired();
        builder.Property(r => r.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsRequired();
        builder.Property(r => r.Position).HasColumnName("position").HasMaxLength(100);
        builder.Property(r => r.Rating).HasColumnName("rating");
        builder.Property(r => r.ReviewText).HasColumnName("review_text").HasMaxLength(2000).IsRequired();
        builder.Property(r => r.IsPublished).HasColumnName("is_published");
        builder.Property(r => r.IsFeatured).HasColumnName("is_featured");
        builder.Property(r => r.SortOrder).HasColumnName("sort_order");
        builder.Property(r => r.SubmitterIp).HasColumnName("submitter_ip");
    }
}
