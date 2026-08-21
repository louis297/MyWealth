using MyWealth.Domain.Constants;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Domain.UnitTests.Constants;

public class RolesTests
{
    [Test]
    public void ShouldDefineLoginCapableAndCustomerRoles()
    {
        Roles.SystemAdmin.ShouldBe("SystemAdmin");
        Roles.TenantAdmin.ShouldBe("TenantAdmin");
        Roles.Adviser.ShouldBe("Adviser");
        Roles.Customer.ShouldBe("Customer");
    }
}
