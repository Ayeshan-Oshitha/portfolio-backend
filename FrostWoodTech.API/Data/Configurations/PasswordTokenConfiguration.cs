using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using FrostWoodTech.API.Entities;

namespace FrostWoodTech.API.Data.Configurations;

public class PasswordTokenConfiguration : IEntityTypeConfiguration<PasswordToken>
{
    public void Configure(EntityTypeBuilder<PasswordToken> builder)
    {
        builder.ToTable("password_tokens");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.UserId).HasColumnName("user_id");
        builder.Property(t => t.TokenHash).HasColumnName("token_hash").IsRequired();
        builder.Property(t => t.Purpose).HasColumnName("purpose").HasColumnType("password_token_purpose");
        builder.Property(t => t.ExpiresAt).HasColumnName("expires_at");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.UsedAt).HasColumnName("used_at");

        // The hash is the lookup key when a link is redeemed; (UserId, Purpose, CreatedAt) backs
        // "invalidate this user's outstanding reset links" without touching their setup link.
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => new { t.UserId, t.Purpose, t.CreatedAt });

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
