using MyWealth.Domain.Constants;
using MyWealth.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Domain.UnitTests.Enums;

public class UserRoleTests
{
    [Test]
    public void ShouldUseStableIntegerValues()
    {
        ((int)UserRole.SystemAdmin).ShouldBe(0);
        ((int)UserRole.TenantAdmin).ShouldBe(1);
        ((int)UserRole.Adviser).ShouldBe(2);
        ((int)UserRole.Customer).ShouldBe(3);
    }

    [Test]
    public void RoleConstantsShouldMatchEnumNames()
    {
        Roles.SystemAdmin.ShouldBe(nameof(UserRole.SystemAdmin));
        Roles.TenantAdmin.ShouldBe(nameof(UserRole.TenantAdmin));
        Roles.Adviser.ShouldBe(nameof(UserRole.Adviser));
        Roles.Customer.ShouldBe(nameof(UserRole.Customer));
    }
}
