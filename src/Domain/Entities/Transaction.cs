using MyWealth.Domain.ValueObjects;

namespace MyWealth.Domain.Entities;

public class Transaction : BaseAuditableEntity
{
    public const int NoteMaxLength = 500;

    public int TenantId { get; private set; }

    public int AccountId { get; private set; }

    public int? HoldingId { get; private set; }

    public DateOnly BookedOn { get; private set; }

    public TransactionType Type { get; private set; }

    public Money Amount { get; private set; } = null!;

    public decimal? Quantity { get; private set; }

    public string? Note { get; private set; }

    private Transaction()
    {
    }

    internal static Transaction Create(
        int tenantId,
        int accountId,
        TransactionType type,
        DateOnly bookedOn,
        Money amount,
        int? holdingId,
        decimal? quantity,
        string? note)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (amount.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        }

        return new Transaction
        {
            TenantId = tenantId,
            AccountId = accountId,
            Type = type,
            BookedOn = bookedOn,
            Amount = amount,
            HoldingId = holdingId,
            Quantity = quantity,
            Note = NormaliseNote(note)
        };
    }

    private static string? NormaliseNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        if (trimmed.Length > NoteMaxLength)
        {
            throw new ArgumentException($"Note must be {NoteMaxLength} characters or fewer.", nameof(note));
        }

        return trimmed;
    }
}
