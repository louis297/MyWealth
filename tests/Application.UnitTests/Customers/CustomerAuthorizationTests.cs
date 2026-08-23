using System.Reflection;
using MyWealth.Application.Common.Security;
using MyWealth.Application.Customers.CreateCustomer;
using MyWealth.Application.Customers.DisableCustomer;
using MyWealth.Application.Customers.GetCustomerById;
using MyWealth.Application.Customers.GetCustomers;
using MyWealth.Application.Customers.UpdateCustomer;
using MyWealth.Domain.Constants;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Customers;

public class CustomerAuthorizationTests
{
    [Test]
    public void CustomerRequests_RequireTenantAdminOrAdviser()
    {
        Type[] types =
        [
            typeof(CreateCustomerCommand),
            typeof(UpdateCustomerCommand),
            typeof(DisableCustomerCommand),
            typeof(GetCustomersQuery),
            typeof(GetCustomerByIdQuery)
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
