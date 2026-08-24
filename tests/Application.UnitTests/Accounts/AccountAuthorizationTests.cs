using System.Reflection;
using MyWealth.Application.Accounts.CloseAccount;
using MyWealth.Application.Accounts.CreateAccount;
using MyWealth.Application.Accounts.GetAccountById;
using MyWealth.Application.Accounts.GetAccounts;
using MyWealth.Application.Accounts.UpdateAccount;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Accounts;

public class AccountAuthorizationTests
{
    [Test]
    public void AccountRequests_RequireTenantAdminOrAdviser()
    {
        Type[] types =
        [
            typeof(CreateAccountCommand),
            typeof(UpdateAccountCommand),
            typeof(CloseAccountCommand),
            typeof(GetAccountsQuery),
            typeof(GetAccountByIdQuery)
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
