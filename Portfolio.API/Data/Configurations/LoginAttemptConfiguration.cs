using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Portfolio.API.Entities;

namespace Portfolio.API.Data.Configurations;

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("login_attempts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.Email).HasColumnName("email").IsRequired();
        builder.Property(a => a.IpAddress).HasColumnName("ip_address");
        builder.Property(a => a.AttemptedAt).HasColumnName("attempted_at");

        // Both counting queries filter on the window, so the timestamp leads each index.
        builder.HasIndex(a => new { a.Email, a.AttemptedAt });
        builder.HasIndex(a => new { a.IpAddress, a.AttemptedAt });
    }
}
