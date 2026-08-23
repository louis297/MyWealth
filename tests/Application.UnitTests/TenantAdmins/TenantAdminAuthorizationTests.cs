using System.Reflection;
using MyWealth.Application.Common.Security;
using MyWealth.Application.TenantAdmins.CreateTenantAdmin;
using MyWealth.Application.TenantAdmins.DisableTenantAdmin;
using MyWealth.Application.TenantAdmins.GetTenantAdminById;
using MyWealth.Application.TenantAdmins.GetTenantAdmins;
using MyWealth.Application.TenantAdmins.UpdateTenantAdmin;
using MyWealth.Domain.Constants;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.TenantAdmins;

public class TenantAdminAuthorizationTests
{
    [Test]
    public void TenantAdminRequests_RequireSystemAdmin()
    {
        Type[] types =
        [
            typeof(CreateTenantAdminCommand),
            typeof(UpdateTenantAdminCommand),
            typeof(DisableTenantAdminCommand),
            typeof(GetTenantAdminsQuery),
            typeof(GetTenantAdminByIdQuery)
        ];

        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<AuthorizeAttribute>();
            attribute.ShouldNotBeNull($"Expected [Authorize] on {type.Name}");
            attribute.Roles.ShouldBe(Roles.SystemAdmin, customMessage: type.Name);
        }
    }
}
