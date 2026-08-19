using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Portfolio.API.Entities;

namespace Portfolio.API.Data.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("articles");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.Title).HasColumnName("title").IsRequired();
        builder.Property(a => a.Excerpt).HasColumnName("excerpt").IsRequired();
        builder.Property(a => a.PublishedDate).HasColumnName("published_date");
        builder.Property(a => a.MediumUrl).HasColumnName("medium_url").IsRequired();
        builder.Property(a => a.CoverImageId).HasColumnName("cover_image_id");
        builder.Property(a => a.Slug).HasColumnName("slug");
        builder.Property(a => a.IsPublished).HasColumnName("is_published");

        builder.HasIndex(a => a.Slug).IsUnique();
    }
}
