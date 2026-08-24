using MyWealth.Domain.Enums;

namespace MyWealth.Application.Dashboard;

public static class DashboardCalculator
{
    public static decimal SignedContribution(TransactionType type, decimal amount)
        => type is TransactionType.TransferOut or TransactionType.Buy ? -amount : amount;

    public static decimal AccountValue(
        AccountType type,
        IEnumerable<(TransactionType Type, decimal Amount)> transactions,
        IEnumerable<decimal> holdingCostBases)
        => type is AccountType.Brokerage or AccountType.Property
            ? holdingCostBases.Sum()
            : transactions.Sum(t => SignedContribution(t.Type, t.Amount));

    public static NetWorthVm ToNetWorth(IEnumerable<AccountContribution> contributions)
    {
        var items = contributions
            .GroupBy(c => c.Currency, StringComparer.Ordinal)
            .Select(group =>
            {
                var assets = group.Where(c => c.AccountType != AccountType.Credit).Sum(c => c.Value);
                var liabilities = group.Where(c => c.AccountType == AccountType.Credit).Sum(c => c.Value);

                return new NetWorthItemVm
                {
                    Currency = group.Key,
                    Assets = assets,
                    Liabilities = liabilities,
                    Net = assets - liabilities
                };
            })
            .OrderBy(item => item.Currency, StringComparer.Ordinal)
            .ToList();

        return new NetWorthVm { Items = items };
    }

    public static AssetAllocationVm ToAllocation(IEnumerable<AccountContribution> contributions)
    {
        var items = contributions
            .GroupBy(c => (c.AccountType, c.Currency))
            .Select(group => new AllocationItemVm
            {
                AccountType = group.Key.AccountType,
                Currency = group.Key.Currency,
                Value = group.Sum(c => c.Value)
            })
            .OrderBy(item => item.AccountType)
            .ThenBy(item => item.Currency, StringComparer.Ordinal)
            .ToList();

        return new AssetAllocationVm { Items = items };
    }
}
