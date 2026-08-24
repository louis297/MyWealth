using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyWealth.Infrastructure.Data.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.Property(a => a.Name)
            .HasMaxLength(Account.NameMaxLength)
            .IsRequired();

        builder.Property(a => a.Currency)
            .HasMaxLength(Account.CurrencyLength)
            .IsFixedLength()
            .IsRequired();

        builder.Property(a => a.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(AccountStatus.Active);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.TenantId, a.CustomerId });

        builder.HasIndex(a => a.CustomerId);

        builder.HasIndex(a => new { a.TenantId, a.Status });

        builder.HasMany(a => a.Holdings)
            .WithOne()
            .HasForeignKey(h => h.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(a => a.Holdings)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
