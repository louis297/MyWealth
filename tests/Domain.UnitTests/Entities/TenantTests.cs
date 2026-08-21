using MyWealth.Domain.Entities;
using MyWealth.Domain.Events;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Domain.UnitTests.Entities;

public class TenantTests
{
    [Test]
    public void Create_TrimsName_EnablesTenant_AndRaisesCreatedEvent()
    {
        var tenant = Tenant.Create("  Acme Wealth  ");

        tenant.Name.ShouldBe("Acme Wealth");
        tenant.IsEnabled.ShouldBeTrue();
        tenant.DomainEvents.Count.ShouldBe(1);
        var created = tenant.DomainEvents.OfType<TenantCreatedEvent>().Single();
        created.Tenant.ShouldBe(tenant);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_RejectsMissingName(string? name)
    {
        var action = () => Tenant.Create(name!);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("name");
    }

    [Test]
    public void Create_RejectsNameLongerThanMaxLength()
    {
        var name = new string('a', Tenant.NameMaxLength + 1);

        var action = () => Tenant.Create(name);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("name");
    }

    [Test]
    public void Rename_UpdatesTrimmedName()
    {
        var tenant = Tenant.Create("Acme");

        tenant.Rename("  New Name  ");

        tenant.Name.ShouldBe("New Name");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Rename_RejectsMissingName(string? name)
    {
        var tenant = Tenant.Create("Acme");

        var action = () => tenant.Rename(name!);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("name");
    }

    [Test]
    public void Rename_RejectsNameLongerThanMaxLength()
    {
        var tenant = Tenant.Create("Acme");
        var name = new string('a', Tenant.NameMaxLength + 1);

        var action = () => tenant.Rename(name);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("name");
    }

    [Test]
    public void DisableThenEnable_RaisesMatchingEvents()
    {
        var tenant = Tenant.Create("Acme");
        tenant.ClearDomainEvents();

        tenant.Disable();
        tenant.IsEnabled.ShouldBeFalse();
        tenant.DomainEvents.OfType<TenantDisabledEvent>().Count().ShouldBe(1);

        tenant.Enable();
        tenant.IsEnabled.ShouldBeTrue();
        tenant.DomainEvents.OfType<TenantEnabledEvent>().Count().ShouldBe(1);
    }

    [Test]
    public void EnableOrDisable_WhenAlreadyInThatState_DoesNotRaiseAnotherEvent()
    {
        var tenant = Tenant.Create("Acme");
        tenant.ClearDomainEvents();

        tenant.Enable();
        tenant.DomainEvents.ShouldBeEmpty();

        tenant.Disable();
        tenant.ClearDomainEvents();
        tenant.Disable();
        tenant.DomainEvents.ShouldBeEmpty();
    }
}
