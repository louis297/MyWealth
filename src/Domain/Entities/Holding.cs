using MyWealth.Domain.ValueObjects;

namespace MyWealth.Domain.Entities;

public class Holding : BaseAuditableEntity
{
    public int TenantId { get; private set; }

    public int AccountId { get; private set; }

    public Instrument Instrument { get; private set; } = null!;

    public decimal Quantity { get; private set; }

    public Money CostBasis { get; private set; } = null!;

    private Holding()
    {
    }

    internal static Holding Create(
        int tenantId,
        int accountId,
        Instrument instrument,
        decimal quantity,
        Money costBasis)
    {
        ArgumentNullException.ThrowIfNull(instrument);
        ArgumentNullException.ThrowIfNull(costBasis);

        var holding = new Holding
        {
            TenantId = tenantId,
            AccountId = accountId,
            Instrument = instrument
        };

        holding.SetQuantity(quantity);
        holding.SetCostBasis(costBasis);
        return holding;
    }

    internal void ChangeInstrument(Instrument instrument)
    {
        ArgumentNullException.ThrowIfNull(instrument);
        Instrument = instrument;
    }

    internal void SetQuantity(decimal quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
        }

        Quantity = quantity;
    }

    internal void SetCostBasisAmount(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Cost basis cannot be negative.", nameof(amount));
        }

        CostBasis = CostBasis.WithAmount(amount);
    }

    private void SetCostBasis(Money costBasis)
    {
        if (costBasis.Amount < 0)
        {
            throw new ArgumentException("Cost basis cannot be negative.", nameof(costBasis));
        }

        CostBasis = costBasis;
    }
}
