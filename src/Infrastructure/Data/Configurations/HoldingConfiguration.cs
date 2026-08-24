using MyWealth.Domain.Entities;
using MyWealth.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyWealth.Infrastructure.Data.Configurations;

public class HoldingConfiguration : IEntityTypeConfiguration<Holding>
{
    public void Configure(EntityTypeBuilder<Holding> builder)
    {
        builder.ToTable("Holdings");

        builder.Property(h => h.Quantity)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.OwnsOne(h => h.Instrument, instrument =>
        {
            instrument.Property(i => i.Name)
                .HasColumnName("Instrument_Name")
                .HasMaxLength(Instrument.NameMaxLength)
                .IsRequired();

            instrument.Property(i => i.Symbol)
                .HasColumnName("Instrument_Symbol")
                .HasMaxLength(Instrument.SymbolMaxLength);
        });

        builder.Navigation(h => h.Instrument).IsRequired();

        builder.OwnsOne(h => h.CostBasis, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("CostBasis_Amount")
                .HasPrecision(18, 4)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("CostBasis_Currency")
                .HasMaxLength(Money.CurrencyLength)
                .IsFixedLength()
                .IsRequired();
        });

        builder.Navigation(h => h.CostBasis).IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(h => h.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => new { h.TenantId, h.AccountId });

        builder.HasIndex(h => h.AccountId);
    }
}
