using MyWealth.Application.Accounts;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Customers;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;

namespace MyWealth.Application.Dashboard;

internal static class DashboardScope
{
    public static async Task<IReadOnlyList<AccountContribution>> LoadContributionsAsync(
        IApplicationDbContext context,
        IUser user,
        int? customerId,
        CancellationToken cancellationToken)
    {
        if (customerId is int id)
        {
            var customer = await CustomerVisibility.FindVisibleCustomerAsync(
                context, user, id, cancellationToken);

            if (customer is null)
            {
                throw new NotFoundException(nameof(User), id);
            }
        }

        var adviserDomainUserId = AccountVisibility.IsAdviser(user)
            ? await AccountVisibility.GetCallerDomainUserIdAsync(context, user, cancellationToken)
            : null;

        var accountsQuery = AccountVisibility.ScopedAccounts(
                context.Accounts.AsNoTracking(),
                context.Users.AsNoTracking(),
                user,
                adviserDomainUserId)
            .Where(a => a.Status == AccountStatus.Active);

        if (customerId is int filterCustomerId)
        {
            accountsQuery = accountsQuery.Where(a => a.CustomerId == filterCustomerId);
        }

        var accounts = await accountsQuery
            .Select(a => new { a.Id, a.Type, a.Currency })
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
        {
            return [];
        }

        var accountIds = accounts.Select(a => a.Id).ToList();

        var transactions = await context.Transactions
            .AsNoTracking()
            .Where(t => accountIds.Contains(t.AccountId))
            .Select(t => new { t.AccountId, t.Type, Amount = t.Amount.Amount })
            .ToListAsync(cancellationToken);

        var holdings = await context.Holdings
            .AsNoTracking()
            .Where(h => accountIds.Contains(h.AccountId))
            .Select(h => new { h.AccountId, Amount = h.CostBasis.Amount })
            .ToListAsync(cancellationToken);

        var transactionsByAccount = transactions.ToLookup(t => t.AccountId);
        var holdingsByAccount = holdings.ToLookup(h => h.AccountId);

        return accounts
            .Select(account => new AccountContribution(
                account.Type,
                account.Currency,
                DashboardCalculator.AccountValue(
                    account.Type,
                    transactionsByAccount[account.Id].Select(t => (t.Type, t.Amount)),
                    holdingsByAccount[account.Id].Select(h => h.Amount))))
            .ToList();
    }
}
