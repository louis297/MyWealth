using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.TenantAdmins;

internal static class TenantAdminProjection
{
    public static IQueryable<User> TenantAdmins(IQueryable<User> users)
        => users.Where(u => u.Role == UserRole.TenantAdmin);

    public static IQueryable<TenantAdminVm> ProjectToVm(IQueryable<User> admins, IQueryable<Tenant> tenants)
        => from admin in admins
           join tenant in tenants on admin.TenantId equals tenant.Id
           select new TenantAdminVm
           {
               Id = admin.Id,
               TenantId = tenant.Id,
               TenantName = tenant.Name,
               Name = admin.Name,
               Email = admin.Email,
               IsEnabled = admin.IsEnabled
           };
}
