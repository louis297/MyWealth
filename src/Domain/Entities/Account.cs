using MyWealth.Domain.Events;
using MyWealth.Domain.ValueObjects;

namespace MyWealth.Domain.Entities;

/// <summary>
/// Aggregate root for a Customer-owned account. Holdings live inside this aggregate.
/// Transactions are added by a later slice.
/// </summary>
public class Account : BaseAuditableEntity
{
    public const int NameMaxLength = 200;
    public const int CurrencyLength = 3;

    private readonly List<Holding> _holdings = [];

    public int TenantId { get; private set; }

    public int CustomerId { get; private set; }

    public string Name { get; private set; } = null!;

    public AccountType Type { get; private set; }

    public AccountStatus Status { get; private set; }

    public string Currency { get; private set; } = null!;

    public IReadOnlyCollection<Holding> Holdings => _holdings.AsReadOnly();

    public bool IsLiability => Type == AccountType.Credit;

    private Account()
    {
    }

    public static Account Open(int tenantId, int customerId, string name, AccountType type, string currency)
    {
        if (tenantId <= 0)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        if (customerId <= 0)
        {
            throw new ArgumentException("Customer is required.", nameof(customerId));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentException("Type is not a valid account type.", nameof(type));
        }

        var account = new Account
        {
            TenantId = tenantId,
            CustomerId = customerId,
            Type = type,
            Status = AccountStatus.Active
        };

        account.SetName(name);
        account.SetCurrency(currency);
        account.AddDomainEvent(new AccountOpenedEvent(account));
        return account;
    }

    public void Rename(string name) => SetName(name);

    public void ChangeType(AccountType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentException("Type is not a valid account type.", nameof(type));
        }

        Type = type;
    }

    public void Close()
    {
        if (Status == AccountStatus.Closed)
        {
            throw new InvalidOperationException("Account is already closed.");
        }

        Status = AccountStatus.Closed;
        AddDomainEvent(new AccountClosedEvent(this));
    }

    public void EnsureWritable()
    {
        if (Status == AccountStatus.Closed)
        {
            throw new InvalidOperationException("Closed accounts reject writes.");
        }
    }

    public Holding AddHolding(Instrument instrument, decimal quantity, Money costBasis)
    {
        EnsureWritable();
        ArgumentNullException.ThrowIfNull(instrument);
        ArgumentNullException.ThrowIfNull(costBasis);

        if (costBasis.Currency != Currency)
        {
            throw new ArgumentException("Cost basis currency must match the account currency.", nameof(costBasis));
        }

        var holding = Holding.Create(TenantId, Id, instrument, quantity, costBasis);
        _holdings.Add(holding);
        AddDomainEvent(new HoldingChangedEvent(this, holding));
        return holding;
    }

    public bool UpdateHolding(int holdingId, Instrument? instrument, decimal? quantity, decimal? costBasisAmount)
    {
        EnsureWritable();

        var holding = _holdings.SingleOrDefault(h => h.Id == holdingId);
        if (holding is null)
        {
            return false;
        }

        if (instrument is not null)
        {
            holding.ChangeInstrument(instrument);
        }

        if (quantity is not null)
        {
            holding.SetQuantity(quantity.Value);
        }

        if (costBasisAmount is not null)
        {
            holding.SetCostBasisAmount(costBasisAmount.Value);
        }

        AddDomainEvent(new HoldingChangedEvent(this, holding));
        return true;
    }

    public bool RemoveHolding(int holdingId)
    {
        EnsureWritable();

        var holding = _holdings.SingleOrDefault(h => h.Id == holdingId);
        if (holding is null)
        {
            return false;
        }

        _holdings.Remove(holding);
        AddDomainEvent(new HoldingChangedEvent(this, holding));
        return true;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new ArgumentException($"Name must be {NameMaxLength} characters or fewer.", nameof(name));
        }

        Name = trimmed;
    }

    private void SetCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        var normalised = currency.Trim().ToUpperInvariant();
        if (normalised.Length != CurrencyLength || !normalised.All(char.IsAsciiLetter))
        {
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));
        }

        Currency = normalised;
    }
}
