using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class ServiceFeatureConfiguration : IEntityTypeConfiguration<ServiceFeature>
{
    public void Configure(EntityTypeBuilder<ServiceFeature> builder)
    {
        builder.ToTable("service_features");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.ServiceId).HasColumnName("service_id");
        builder.Property(f => f.Title).HasColumnName("title").IsRequired();
        builder.Property(f => f.Description).HasColumnName("description");
        builder.Property(f => f.IconName).HasColumnName("icon_name");
        builder.Property(f => f.SortOrder).HasColumnName("sort_order");

        builder.HasOne(f => f.Service)
            .WithMany(s => s.Features)
            .HasForeignKey(f => f.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
