namespace MyWealth.Application.Dashboard;

public sealed class NetWorthVm
{
    public required IReadOnlyList<NetWorthItemVm> Items { get; init; }
}

public sealed class NetWorthItemVm
{
    public required string Currency { get; init; }

    public decimal Assets { get; init; }

    public decimal Liabilities { get; init; }

    public decimal Net { get; init; }
}
