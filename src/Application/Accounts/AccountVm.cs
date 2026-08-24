using MyWealth.Domain.Enums;

namespace MyWealth.Application.Accounts;

public sealed class AccountVm
{
    public int Id { get; init; }

    public int CustomerId { get; init; }

    public required string CustomerName { get; init; }

    public required string Name { get; init; }

    public AccountType Type { get; init; }

    public AccountStatus Status { get; init; }

    public required string Currency { get; init; }
}
