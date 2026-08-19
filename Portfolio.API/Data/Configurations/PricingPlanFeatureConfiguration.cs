using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Portfolio.API.Entities;

namespace Portfolio.API.Data.Configurations;

public class PricingPlanFeatureConfiguration : IEntityTypeConfiguration<PricingPlanFeature>
{
    public void Configure(EntityTypeBuilder<PricingPlanFeature> builder)
    {
        builder.ToTable("pricing_plan_features");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");
        builder.Property(f => f.PricingPlanId).HasColumnName("pricing_plan_id");
        builder.Property(f => f.Text).HasColumnName("text").IsRequired();
        builder.Property(f => f.IsIncluded).HasColumnName("is_included");
        builder.Property(f => f.SortOrder).HasColumnName("sort_order");

        builder.HasOne(f => f.PricingPlan)
            .WithMany(p => p.Features)
            .HasForeignKey(f => f.PricingPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
