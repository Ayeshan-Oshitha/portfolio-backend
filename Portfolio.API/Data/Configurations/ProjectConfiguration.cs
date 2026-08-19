using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Portfolio.API.Entities;

namespace Portfolio.API.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.Slug).HasColumnName("slug").IsRequired();
        builder.Property(p => p.Title).HasColumnName("title").IsRequired();
        builder.Property(p => p.Year).HasColumnName("year");
        builder.Property(p => p.ShortDescription).HasColumnName("short_description").IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").IsRequired();
        builder.Property(p => p.WebsiteUrl).HasColumnName("website_url");
        builder.Property(p => p.Problem).HasColumnName("problem");
        builder.Property(p => p.Solution).HasColumnName("solution");
        builder.Property(p => p.WhatWeDelivered).HasColumnName("what_we_delivered");
        builder.Property(p => p.Proof).HasColumnName("proof");
        builder.Property(p => p.ClientName).HasColumnName("client_name");
        builder.Property(p => p.IsPublished).HasColumnName("is_published");
        builder.Property(p => p.PublishedAt).HasColumnName("published_at");
        builder.Property(p => p.SeoTitle).HasColumnName("seo_title");
        builder.Property(p => p.SeoDescription).HasColumnName("seo_description");

        builder.HasIndex(p => p.Slug).IsUnique();
    }
}
