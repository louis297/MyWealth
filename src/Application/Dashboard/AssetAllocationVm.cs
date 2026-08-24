using MyWealth.Domain.Enums;

namespace MyWealth.Application.Dashboard;

public sealed class AssetAllocationVm
{
    public required IReadOnlyList<AllocationItemVm> Items { get; init; }
}

public sealed class AllocationItemVm
{
    public AccountType AccountType { get; init; }

    public required string Currency { get; init; }

    public decimal Value { get; init; }
}
