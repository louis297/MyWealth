using System.Reflection;
using MyWealth.Application.Advisers.CreateAdviser;
using MyWealth.Application.Advisers.DisableAdviser;
using MyWealth.Application.Advisers.GetAdviserById;
using MyWealth.Application.Advisers.GetAdvisers;
using MyWealth.Application.Advisers.UpdateAdviser;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Advisers;

public class AdviserAuthorizationTests
{
    [Test]
    public void AdviserRequests_RequireTenantAdmin()
    {
        Type[] types =
        [
            typeof(CreateAdviserCommand),
            typeof(UpdateAdviserCommand),
            typeof(DisableAdviserCommand),
            typeof(GetAdvisersQuery),
            typeof(GetAdviserByIdQuery)
        ];

        foreach (var type in types)
        {
            var attribute = type.GetCustomAttribute<AuthorizeAttribute>();
            attribute.ShouldNotBeNull($"Expected [Authorize] on {type.Name}");
            attribute.Roles.ShouldBe(Roles.TenantAdmin, customMessage: type.Name);
        }
    }
}
