using MyWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyWealth.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(u => u.Name)
            .HasMaxLength(User.NameMaxLength)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(User.EmailMaxLength)
            .IsRequired()
            .UseCollation("SQL_Latin1_General_CP1_CI_AS");

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(u => u.IdentityUserId)
            .HasMaxLength(450);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(u => u.AdviserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => new { u.TenantId, u.Role });

        builder.HasIndex(u => u.AdviserId);

        builder.HasIndex(u => u.IdentityUserId)
            .IsUnique()
            .HasFilter("[IdentityUserId] IS NOT NULL");
    }
}
