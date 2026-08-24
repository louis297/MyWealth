using System.Reflection;
using MyWealth.Application.Common.Security;
using MyWealth.Application.Holdings.CreateHolding;
using MyWealth.Application.Holdings.DeleteHolding;
using MyWealth.Application.Holdings.GetHoldingById;
using MyWealth.Application.Holdings.GetHoldingsByAccount;
using MyWealth.Application.Holdings.UpdateHolding;
using MyWealth.Domain.Constants;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Holdings;

public class HoldingAuthorizationTests
{
    [Test]
    public void HoldingRequests_RequireTenantAdminOrAdviser()
    {
        Type[] types =
        [
            typeof(CreateHoldingCommand),
            typeof(UpdateHoldingCommand),
            typeof(DeleteHoldingCommand),
            typeof(GetHoldingsByAccountQuery),
            typeof(GetHoldingByIdQuery)
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
