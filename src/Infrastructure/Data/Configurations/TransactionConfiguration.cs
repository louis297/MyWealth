using MyWealth.Domain.Entities;
using MyWealth.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyWealth.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.Property(t => t.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.BookedOn)
            .IsRequired();

        builder.Property(t => t.Quantity)
            .HasPrecision(18, 8);

        builder.Property(t => t.Note)
            .HasMaxLength(Transaction.NoteMaxLength);

        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Amount_Amount")
                .HasPrecision(18, 4)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Amount_Currency")
                .HasMaxLength(Money.CurrencyLength)
                .IsFixedLength()
                .IsRequired();
        });

        builder.Navigation(t => t.Amount).IsRequired();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Holding>()
            .WithMany()
            .HasForeignKey(t => t.HoldingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.TenantId, t.AccountId, t.BookedOn });

        builder.HasIndex(t => new { t.AccountId, t.Type });

        builder.HasIndex(t => t.HoldingId);
    }
}
