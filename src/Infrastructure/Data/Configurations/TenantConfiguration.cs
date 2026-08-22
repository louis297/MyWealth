using MyWealth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyWealth.Infrastructure.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.Property(t => t.Name)
            .HasMaxLength(Tenant.NameMaxLength)
            .IsRequired()
            .UseCollation("SQL_Latin1_General_CP1_CI_AS");

        builder.Property(t => t.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(t => t.Name)
            .IsUnique();
    }
}
