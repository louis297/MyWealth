using MyWealth.Application.Common.Interfaces;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.Customers;

internal static class CustomerVisibility
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

    public static IQueryable<User> ScopedCustomers(
        IQueryable<User> users,
        IUser user,
        int? adviserDomainUserId)
    {
        var query = users.Where(u => u.Role == UserRole.Customer);

        if (!IsAdviser(user))
        {
            return query;
        }

        var id = adviserDomainUserId ?? -1;
        return query.Where(u => u.AdviserId == id);
    }

    public static async Task<User?> FindVisibleCustomerAsync(
        IApplicationDbContext context,
        IUser user,
        int id,
        CancellationToken cancellationToken)
    {
        var adviserDomainUserId = IsAdviser(user)
            ? await GetCallerDomainUserIdAsync(context, user, cancellationToken)
            : null;

        return await ScopedCustomers(context.Users, user, adviserDomainUserId)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public static IQueryable<CustomerVm> ProjectToVm(IQueryable<User> customers, IQueryable<User> users)
        => from customer in customers
           join adviser in users on customer.AdviserId equals adviser.Id
           select new CustomerVm
           {
               Id = customer.Id,
               Name = customer.Name,
               Email = customer.Email,
               IsEnabled = customer.IsEnabled,
               AdviserId = customer.AdviserId!.Value,
               AdviserName = adviser.Name
           };

    public static Task<bool> IsEnabledAdviserAsync(
        IApplicationDbContext context,
        int adviserId,
        CancellationToken cancellationToken)
        => context.Users.AnyAsync(
            u => u.Id == adviserId && u.Role == UserRole.Adviser && u.IsEnabled,
            cancellationToken);

    public static async Task<bool> CallerMayAssignAsync(
        IApplicationDbContext context,
        IUser user,
        int adviserId,
        CancellationToken cancellationToken)
    {
        if (!IsAdviser(user))
        {
            return true;
        }

        var domainUserId = await GetCallerDomainUserIdAsync(context, user, cancellationToken);
        return domainUserId == adviserId;
    }
}
