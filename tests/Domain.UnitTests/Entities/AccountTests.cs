using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using MyWealth.Domain.Events;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Domain.UnitTests.Entities;

public class AccountTests
{
    [Test]
    public void Open_TrimsName_NormalisesCurrency_SetsActive_AndRaisesOpenedEvent()
    {
        var account = Account.Open(1, 42, "  Primary Brokerage  ", AccountType.Brokerage, " nzd ");

        account.TenantId.ShouldBe(1);
        account.CustomerId.ShouldBe(42);
        account.Name.ShouldBe("Primary Brokerage");
        account.Type.ShouldBe(AccountType.Brokerage);
        account.Status.ShouldBe(AccountStatus.Active);
        account.Currency.ShouldBe("NZD");
        account.IsLiability.ShouldBeFalse();
        account.DomainEvents.Count.ShouldBe(1);
        account.DomainEvents.OfType<AccountOpenedEvent>().Single().Account.ShouldBe(account);
    }

    [Test]
    public void Open_CreditType_IsLiability()
    {
        var account = Account.Open(1, 42, "Visa", AccountType.Credit, "NZD");

        account.IsLiability.ShouldBeTrue();
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Open_RejectsMissingTenant(int tenantId)
    {
        var action = () => Account.Open(tenantId, 42, "Cash", AccountType.Cash, "NZD");

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("tenantId");
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Open_RejectsMissingCustomer(int customerId)
    {
        var action = () => Account.Open(1, customerId, "Cash", AccountType.Cash, "NZD");

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("customerId");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Open_RejectsMissingName(string? name)
    {
        var action = () => Account.Open(1, 42, name!, AccountType.Cash, "NZD");

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("name");
    }

    [Test]
    public void Open_RejectsNameLongerThanMaxLength()
    {
        var name = new string('a', Account.NameMaxLength + 1);

        var action = () => Account.Open(1, 42, name, AccountType.Cash, "NZD");

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("name");
    }

    [Test]
    public void Open_RejectsUndefinedType()
    {
        var action = () => Account.Open(1, 42, "Cash", (AccountType)99, "NZD");

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("type");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("NZ")]
    [TestCase("NZDD")]
    [TestCase("N1D")]
    [TestCase("12D")]
    public void Open_RejectsInvalidCurrency(string? currency)
    {
        var action = () => Account.Open(1, 42, "Cash", AccountType.Cash, currency!);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("currency");
    }

    [Test]
    public void Rename_UpdatesTrimmedName()
    {
        var account = Account.Open(1, 42, "Cash", AccountType.Cash, "NZD");

        account.Rename("  Everyday Cash  ");

        account.Name.ShouldBe("Everyday Cash");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Rename_RejectsMissingName(string? name)
    {
        var account = Account.Open(1, 42, "Cash", AccountType.Cash, "NZD");

        var action = () => account.Rename(name!);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("name");
    }

    [Test]
    public void ChangeType_UpdatesType()
    {
        var account = Account.Open(1, 42, "Main", AccountType.Bank, "NZD");

        account.ChangeType(AccountType.Brokerage);

        account.Type.ShouldBe(AccountType.Brokerage);
        account.Currency.ShouldBe("NZD");
    }

    [Test]
    public void ChangeType_RejectsUndefinedType()
    {
        var account = Account.Open(1, 42, "Main", AccountType.Bank, "NZD");

        var action = () => account.ChangeType((AccountType)99);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("type");
        account.Type.ShouldBe(AccountType.Bank);
    }

    [Test]
    public void Close_SetsClosed_AndRaisesClosedEvent()
    {
        var account = Account.Open(1, 42, "Cash", AccountType.Cash, "NZD");
        account.ClearDomainEvents();

        account.Close();

        account.Status.ShouldBe(AccountStatus.Closed);
        account.DomainEvents.OfType<AccountClosedEvent>().Single().Account.ShouldBe(account);
    }

    [Test]
    public void Close_WhenAlreadyClosed_Throws()
    {
        var account = Account.Open(1, 42, "Cash", AccountType.Cash, "NZD");
        account.Close();
        account.ClearDomainEvents();

        var action = () => account.Close();

        Should.Throw<InvalidOperationException>(action).Message.ShouldContain("already closed");
        account.Status.ShouldBe(AccountStatus.Closed);
        account.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public void RenameAndChangeType_AllowedWhenClosed()
    {
        var account = Account.Open(1, 42, "Cash", AccountType.Cash, "NZD");
        account.Close();

        account.Rename("Closed Cash");
        account.ChangeType(AccountType.Bank);

        account.Name.ShouldBe("Closed Cash");
        account.Type.ShouldBe(AccountType.Bank);
        account.Status.ShouldBe(AccountStatus.Closed);
        account.Currency.ShouldBe("NZD");
    }

    [Test]
    public void EnsureWritable_WhenActive_DoesNotThrow()
    {
        var account = Account.Open(1, 42, "Cash", AccountType.Cash, "NZD");

        Should.NotThrow(() => account.EnsureWritable());
    }

    [Test]
    public void EnsureWritable_WhenClosed_Throws()
    {
        var account = Account.Open(1, 42, "Cash", AccountType.Cash, "NZD");
        account.Close();

        Should.Throw<InvalidOperationException>(() => account.EnsureWritable())
            .Message.ShouldContain("Closed accounts reject writes");
    }
}
