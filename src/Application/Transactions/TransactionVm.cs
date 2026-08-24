using MyWealth.Application.Holdings;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.Transactions;

public sealed class TransactionVm
{
    public int Id { get; init; }

    public int AccountId { get; init; }

    public int? HoldingId { get; init; }

    public DateOnly BookedOn { get; init; }

    public TransactionType Type { get; init; }

    public required MoneyVm Amount { get; init; }

    public decimal? Quantity { get; init; }

    public string? Note { get; init; }
}
