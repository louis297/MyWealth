using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using MyWealth.Domain.Events;
using MyWealth.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Domain.UnitTests.Entities;

public class HoldingTests
{
    [Test]
    public void AddHolding_AddsPosition_AndRaisesChangedEvent()
    {
        var account = OpenAccount();
        account.ClearDomainEvents();

        var holding = account.AddHolding(
            Instrument.Create("  Apple Inc.  ", "  AAPL  "),
            100m,
            Money.Of(18500m, "NZD"));

        holding.TenantId.ShouldBe(account.TenantId);
        holding.AccountId.ShouldBe(account.Id);
        holding.Instrument.Name.ShouldBe("Apple Inc.");
        holding.Instrument.Symbol.ShouldBe("AAPL");
        holding.Quantity.ShouldBe(100m);
        holding.CostBasis.ShouldBe(Money.Of(18500m, "NZD"));
        account.Holdings.ShouldContain(holding);
        account.DomainEvents.OfType<HoldingChangedEvent>().Single().Holding.ShouldBe(holding);
    }

    [Test]
    public void AddHolding_AllowsZeroQuantity()
    {
        var account = OpenAccount();

        var holding = account.AddHolding(Instrument.Create("Cash Buffer"), 0m, Money.Of(0m, "NZD"));

        holding.Quantity.ShouldBe(0m);
        holding.CostBasis.Amount.ShouldBe(0m);
    }

    [Test]
    public void AddHolding_RejectsNegativeQuantity()
    {
        var account = OpenAccount();

        var action = () => account.AddHolding(Instrument.Create("Apple Inc."), -1m, Money.Of(1m, "NZD"));

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("quantity");
        account.Holdings.ShouldBeEmpty();
    }

    [Test]
    public void AddHolding_RejectsNegativeCostBasis()
    {
        var account = OpenAccount();

        var action = () => account.AddHolding(Instrument.Create("Apple Inc."), 1m, Money.Of(-1m, "NZD"));

        Should.Throw<ArgumentException>(action);
        account.Holdings.ShouldBeEmpty();
    }

    [Test]
    public void AddHolding_RejectsCurrencyMismatch()
    {
        var account = OpenAccount();

        var action = () => account.AddHolding(Instrument.Create("Apple Inc."), 1m, Money.Of(1m, "USD"));

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("costBasis");
        account.Holdings.ShouldBeEmpty();
    }

    [Test]
    public void AddHolding_WhenClosed_Throws()
    {
        var account = OpenAccount();
        account.Close();

        var action = () => account.AddHolding(Instrument.Create("Apple Inc."), 1m, Money.Of(1m, "NZD"));

        Should.Throw<InvalidOperationException>(action).Message.ShouldContain("Closed accounts reject writes");
        account.Holdings.ShouldBeEmpty();
    }

    [Test]
    public void UpdateHolding_UpdatesSuppliedFields_KeepsCurrency()
    {
        var account = OpenAccount();
        var holding = account.AddHolding(Instrument.Create("Apple Inc.", "AAPL"), 100m, Money.Of(18500m, "NZD"));
        holding.Id = 5;
        account.ClearDomainEvents();

        var updated = account.UpdateHolding(
            5,
            Instrument.Create("Apple Inc.", "AAPL"),
            120m,
            19200m);

        updated.ShouldBeTrue();
        holding.Instrument.Name.ShouldBe("Apple Inc.");
        holding.Quantity.ShouldBe(120m);
        holding.CostBasis.Amount.ShouldBe(19200m);
        holding.CostBasis.Currency.ShouldBe("NZD");
        account.DomainEvents.OfType<HoldingChangedEvent>().ShouldNotBeEmpty();
    }

    [Test]
    public void UpdateHolding_AmountOnly_KeepsInstrumentAndQuantity()
    {
        var account = OpenAccount();
        var holding = account.AddHolding(Instrument.Create("Apple Inc.", "AAPL"), 100m, Money.Of(18500m, "NZD"));
        holding.Id = 5;

        account.UpdateHolding(5, null, null, 20000m).ShouldBeTrue();

        holding.Instrument.Name.ShouldBe("Apple Inc.");
        holding.Quantity.ShouldBe(100m);
        holding.CostBasis.Amount.ShouldBe(20000m);
        holding.CostBasis.Currency.ShouldBe("NZD");
    }

    [Test]
    public void UpdateHolding_MissingHolding_ReturnsFalse()
    {
        var account = OpenAccount();

        account.UpdateHolding(99, Instrument.Create("X"), 1m, 1m).ShouldBeFalse();
        account.Holdings.ShouldBeEmpty();
    }

    [Test]
    public void UpdateHolding_WhenClosed_Throws()
    {
        var account = OpenAccount();
        var holding = account.AddHolding(Instrument.Create("Apple Inc."), 1m, Money.Of(1m, "NZD"));
        holding.Id = 5;
        account.Close();

        var action = () => { account.UpdateHolding(5, null, 2m, null); };

        Should.Throw<InvalidOperationException>(action).Message.ShouldContain("Closed accounts reject writes");
        holding.Quantity.ShouldBe(1m);
    }

    [Test]
    public void RemoveHolding_RemovesAndRaisesEvent()
    {
        var account = OpenAccount();
        var holding = account.AddHolding(Instrument.Create("Apple Inc."), 1m, Money.Of(1m, "NZD"));
        holding.Id = 5;
        account.ClearDomainEvents();

        account.RemoveHolding(5).ShouldBeTrue();

        account.Holdings.ShouldBeEmpty();
        account.DomainEvents.OfType<HoldingChangedEvent>().Single().Holding.ShouldBe(holding);
    }

    [Test]
    public void RemoveHolding_MissingHolding_ReturnsFalse()
    {
        var account = OpenAccount();

        account.RemoveHolding(99).ShouldBeFalse();
    }

    [Test]
    public void RemoveHolding_WhenClosed_Throws()
    {
        var account = OpenAccount();
        var holding = account.AddHolding(Instrument.Create("Apple Inc."), 1m, Money.Of(1m, "NZD"));
        holding.Id = 5;
        account.Close();

        var action = () => { account.RemoveHolding(5); };

        Should.Throw<InvalidOperationException>(action).Message.ShouldContain("Closed accounts reject writes");
        account.Holdings.ShouldContain(holding);
    }

    private static Account OpenAccount() => Account.Open(1, 42, "Primary Brokerage", AccountType.Brokerage, "NZD");
}
