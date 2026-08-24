using MyWealth.Application.Common.Interfaces;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.Accounts;

internal static class AccountVisibility
{
    public const string AllowedRoles = Roles.TenantAdmin + "," + Roles.Adviser;

    public static bool IsAdviser(IUser user)
        => user.Roles?.Contains(Roles.Adviser) == true;

    public static async Task<int?> GetCallerDomainUserIdAsync(
        IApplicationDbContext context,
        IUser user,
        CancellationToken cancellationToken)
    {
        if (user.Id is null)
        {
            return null;
        }

        return await context.Users
            .AsNoTracking()
            .Where(u => u.IdentityUserId == user.Id)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static IQueryable<Account> ScopedAccounts(
        IQueryable<Account> accounts,
        IQueryable<User> users,
        IUser user,
        int? adviserDomainUserId)
    {
        if (!IsAdviser(user))
        {
            return accounts;
        }

        var id = adviserDomainUserId ?? -1;
        return from account in accounts
               join customer in users on account.CustomerId equals customer.Id
               where customer.AdviserId == id
               select account;
    }

    public static async Task<Account?> FindVisibleAccountAsync(
        IApplicationDbContext context,
        IUser user,
        int id,
        CancellationToken cancellationToken)
    {
        var adviserDomainUserId = IsAdviser(user)
            ? await GetCallerDomainUserIdAsync(context, user, cancellationToken)
            : null;

        return await ScopedAccounts(context.Accounts, context.Users, user, adviserDomainUserId)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public static async Task<Account?> FindVisibleAccountAggregateAsync(
        IApplicationDbContext context,
        IUser user,
        int id,
        CancellationToken cancellationToken)
    {
        IQueryable<Account> accounts = context.Accounts
            .Include(a => a.Holdings)
            .Include(a => a.Transactions);

        if (IsAdviser(user))
        {
            var adviserDomainUserId = await GetCallerDomainUserIdAsync(context, user, cancellationToken) ?? -1;
            accounts = accounts.Where(a =>
                context.Users.Any(c => c.Id == a.CustomerId && c.AdviserId == adviserDomainUserId));
        }

        return await accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public static IQueryable<AccountVm> ProjectToVm(IQueryable<Account> accounts, IQueryable<User> users)
        => from account in accounts
           join customer in users on account.CustomerId equals customer.Id
           select new AccountVm
           {
               Id = account.Id,
               CustomerId = account.CustomerId,
               CustomerName = customer.Name,
               Name = account.Name,
               Type = account.Type,
               Status = account.Status,
               Currency = account.Currency
           };

    public static Task<bool> IsEnabledCustomerInTenantAsync(
        IApplicationDbContext context,
        int customerId,
        CancellationToken cancellationToken)
        => context.Users.AnyAsync(
            u => u.Id == customerId && u.Role == UserRole.Customer && u.IsEnabled,
            cancellationToken);

    public static async Task<bool> CallerMayTargetCustomerAsync(
        IApplicationDbContext context,
        IUser user,
        int customerId,
        CancellationToken cancellationToken)
    {
        if (!IsAdviser(user))
        {
            return true;
        }

        var domainUserId = await GetCallerDomainUserIdAsync(context, user, cancellationToken);
        return await context.Users.AnyAsync(
            u => u.Id == customerId && u.Role == UserRole.Customer && u.AdviserId == domainUserId,
            cancellationToken);
    }
}
