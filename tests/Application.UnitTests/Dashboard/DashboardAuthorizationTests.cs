using System.Reflection;
using MyWealth.Application.Common.Security;
using MyWealth.Application.Dashboard.GetAssetAllocation;
using MyWealth.Application.Dashboard.GetNetWorth;
using MyWealth.Domain.Constants;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Dashboard;

public class DashboardAuthorizationTests
{
    [Test]
    public void DashboardRequests_RequireTenantAdminOrAdviser()
    {
        Type[] types =
        [
            typeof(GetNetWorthQuery),
            typeof(GetAssetAllocationQuery)
        ];

        var expected = Roles.TenantAdmin + "," + Roles.Adviser;

        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<AuthorizeAttribute>();
            attribute.ShouldNotBeNull($"Expected [Authorize] on {type.Name}");
            attribute.Roles.ShouldBe(expected, customMessage: type.Name);
        }
    }
}
