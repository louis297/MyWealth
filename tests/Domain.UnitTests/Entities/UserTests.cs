using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using MyWealth.Domain.Events;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Domain.UnitTests.Entities;

public class UserTests
{
    [Test]
    public void CreateAdviser_TrimsFields_EnablesUser_AndRaisesCreatedEvent()
    {
        var user = User.CreateAdviser(1, "  Jane Smith  ", "  jane@acme.com  ");

        user.TenantId.ShouldBe(1);
        user.Name.ShouldBe("Jane Smith");
        user.Email.ShouldBe("jane@acme.com");
        user.IsEnabled.ShouldBeTrue();
        user.Role.ShouldBe(UserRole.Adviser);
        user.AdviserId.ShouldBeNull();
        user.IdentityUserId.ShouldBeNull();
        user.DomainEvents.Count.ShouldBe(1);
        user.DomainEvents.OfType<UserCreatedEvent>().Single().User.ShouldBe(user);
    }

    [Test]
    public void CreateSystemAdmin_HasNoTenantOrAdviser()
    {
        var user = User.CreateSystemAdmin("Platform Admin", "sa@localhost");

        user.Role.ShouldBe(UserRole.SystemAdmin);
        user.TenantId.ShouldBeNull();
        user.AdviserId.ShouldBeNull();
    }

    [Test]
    public void CreateTenantAdmin_RequiresTenant_AndHasNoAdviser()
    {
        var user = User.CreateTenantAdmin(4, "Tenant Admin", "ta@acme.com");

        user.Role.ShouldBe(UserRole.TenantAdmin);
        user.TenantId.ShouldBe(4);
        user.AdviserId.ShouldBeNull();
    }

    [Test]
    public void CreateCustomer_RequiresTenantAndAdviser()
    {
        var user = User.CreateCustomer(1, 12, "Zhang San", "zhangsan@example.com");

        user.Role.ShouldBe(UserRole.Customer);
        user.TenantId.ShouldBe(1);
        user.AdviserId.ShouldBe(12);
        user.IdentityUserId.ShouldBeNull();
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CreateTenantAdmin_RejectsMissingTenant(int tenantId)
    {
        var action = () => User.CreateTenantAdmin(tenantId, "Admin", "ta@acme.com");

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("tenantId");
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CreateCustomer_RejectsMissingTenant(int tenantId)
    {
        var action = () => User.CreateCustomer(tenantId, 12, "Zhang", "zhang@example.com");

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("tenantId");
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CreateAdviser_RejectsMissingTenant(int tenantId)
    {
        var action = () => User.CreateAdviser(tenantId, "Jane", "jane@acme.com");

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("tenantId");
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void CreateCustomer_RejectsMissingAdviser(int adviserId)
    {
        var action = () => User.CreateCustomer(1, adviserId, "Zhang", "zhang@example.com");

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("adviserId");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void CreateAdviser_RejectsMissingName(string? name)
    {
        var action = () => User.CreateAdviser(1, name!, "jane@acme.com");

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("name");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void CreateAdviser_RejectsMissingEmail(string? email)
    {
        var action = () => User.CreateAdviser(1, "Jane", email!);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("email");
    }

    [Test]
    public void CreateAdviser_RejectsNameLongerThanMaxLength()
    {
        var name = new string('a', User.NameMaxLength + 1);

        var action = () => User.CreateAdviser(1, name, "jane@acme.com");

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("name");
    }

    [Test]
    public void CreateAdviser_RejectsEmailLongerThanMaxLength()
    {
        var email = new string('a', User.EmailMaxLength - 3) + "@x.com";
        email.Length.ShouldBeGreaterThan(User.EmailMaxLength);

        var action = () => User.CreateAdviser(1, "Jane", email);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("email");
    }

    [Test]
    public void ReassignAdviser_UpdatesId_AndRaisesEvent()
    {
        var user = User.CreateCustomer(1, 12, "Zhang", "zhang@example.com");
        user.ClearDomainEvents();

        user.ReassignAdviser(34);

        user.AdviserId.ShouldBe(34);
        user.DomainEvents.OfType<CustomerReassignedEvent>().Single().User.ShouldBe(user);
    }

    [Test]
    public void ReassignAdviser_WhenUnchanged_DoesNotRaiseEvent()
    {
        var user = User.CreateCustomer(1, 12, "Zhang", "zhang@example.com");
        user.ClearDomainEvents();

        user.ReassignAdviser(12);

        user.AdviserId.ShouldBe(12);
        user.DomainEvents.ShouldBeEmpty();
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void ReassignAdviser_RejectsMissingAdviser(int adviserId)
    {
        var user = User.CreateCustomer(1, 12, "Zhang", "zhang@example.com");

        var action = () => user.ReassignAdviser(adviserId);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("adviserId");
        user.AdviserId.ShouldBe(12);
    }

    [Test]
    public void ReassignAdviser_OnNonCustomer_Throws()
    {
        var user = User.CreateAdviser(1, "Jane", "jane@acme.com");

        var action = () => user.ReassignAdviser(34);

        Should.Throw<InvalidOperationException>(action)
            .Message.ShouldContain("customers");
        user.AdviserId.ShouldBeNull();
    }

    [Test]
    public void Rename_UpdatesTrimmedName()
    {
        var user = User.CreateAdviser(1, "Jane", "jane@acme.com");

        user.Rename("  Jane Smith  ");

        user.Name.ShouldBe("Jane Smith");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Rename_RejectsMissingName(string? name)
    {
        var user = User.CreateAdviser(1, "Jane", "jane@acme.com");

        var action = () => user.Rename(name!);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("name");
    }

    [Test]
    public void DisableThenEnable_RaisesMatchingEvents()
    {
        var user = User.CreateAdviser(1, "Jane", "jane@acme.com");
        user.ClearDomainEvents();

        user.Disable();
        user.IsEnabled.ShouldBeFalse();
        user.DomainEvents.OfType<UserDisabledEvent>().Count().ShouldBe(1);

        user.Enable();
        user.IsEnabled.ShouldBeTrue();
        user.DomainEvents.OfType<UserEnabledEvent>().Count().ShouldBe(1);
    }

    [Test]
    public void EnableOrDisable_WhenAlreadyInThatState_DoesNotRaiseAnotherEvent()
    {
        var user = User.CreateAdviser(1, "Jane", "jane@acme.com");
        user.ClearDomainEvents();

        user.Enable();
        user.DomainEvents.ShouldBeEmpty();

        user.Disable();
        user.ClearDomainEvents();
        user.Disable();
        user.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public void Disable_AdviserWithAssignedCustomers_Throws()
    {
        var user = User.CreateAdviser(1, "Jane", "jane@acme.com");

        var action = () => user.Disable(assignedCustomerCount: 1);

        Should.Throw<InvalidOperationException>(action)
            .Message.ShouldContain("customers");
        user.IsEnabled.ShouldBeTrue();
    }

    [Test]
    public void Disable_NonAdviserWithAssignedCustomerCount_DoesNotThrow()
    {
        var user = User.CreateCustomer(1, 12, "Zhang", "zhang@example.com");

        user.Disable(assignedCustomerCount: 1);

        user.IsEnabled.ShouldBeFalse();
    }

    [Test]
    public void LinkIdentity_SetsTrimmedId()
    {
        var user = User.CreateAdviser(1, "Jane", "jane@acme.com");

        user.LinkIdentity("  identity-1  ");

        user.IdentityUserId.ShouldBe("identity-1");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void LinkIdentity_RejectsMissingId(string? identityUserId)
    {
        var user = User.CreateAdviser(1, "Jane", "jane@acme.com");

        var action = () => user.LinkIdentity(identityUserId!);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("identityUserId");
    }
}
