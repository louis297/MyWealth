using System.Reflection;
using MyWealth.Application.Common.Security;
using MyWealth.Application.Tenants.CreateTenant;
using MyWealth.Application.Tenants.GetTenantById;
using MyWealth.Application.Tenants.GetTenants;
using MyWealth.Application.Tenants.UpdateTenant;
using MyWealth.Domain.Constants;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Tenants;

public class TenantAuthorizationTests
{
    [Test]
    public void TenantRequests_RequireSystemAdmin()
    {
        Type[] types =
        [
            typeof(CreateTenantCommand),
            typeof(UpdateTenantCommand),
            typeof(GetTenantsQuery),
            typeof(GetTenantByIdQuery)
        ];

        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<AuthorizeAttribute>();
            attribute.ShouldNotBeNull($"Expected [Authorize] on {type.Name}");
            attribute.Roles.ShouldBe(Roles.SystemAdmin, customMessage: type.Name);
        }
    }
}
