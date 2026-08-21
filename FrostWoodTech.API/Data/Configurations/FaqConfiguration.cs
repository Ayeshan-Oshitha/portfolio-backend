using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class FaqConfiguration : IEntityTypeConfiguration<Faq>
{
    public void Configure(EntityTypeBuilder<Faq> builder)
    {
        builder.ToTable("faqs");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.Question).HasColumnName("question").IsRequired();
        builder.Property(f => f.Answer).HasColumnName("answer").IsRequired();
        builder.Property(f => f.Category).HasColumnName("category");
        builder.Property(f => f.SortOrder).HasColumnName("sort_order");
        builder.Property(f => f.IsPublished).HasColumnName("is_published");
    }
}
