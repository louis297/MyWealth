using System.Reflection;
using MyWealth.Application.Common.Security;
using MyWealth.Application.Transactions.CreateTransaction;
using MyWealth.Application.Transactions.GetTransactionById;
using MyWealth.Application.Transactions.GetTransactions;
using MyWealth.Domain.Constants;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Transactions;

public class TransactionAuthorizationTests
{
    [Test]
    public void TransactionRequests_RequireTenantAdminOrAdviser()
    {
        Type[] types =
        [
            typeof(CreateTransactionCommand),
            typeof(GetTransactionsQuery),
            typeof(GetTransactionByIdQuery)
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
