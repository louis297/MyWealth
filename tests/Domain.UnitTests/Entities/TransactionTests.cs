using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using MyWealth.Domain.Events;
using MyWealth.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Domain.UnitTests.Entities;

public class TransactionTests
{
    private static readonly DateOnly BookedOn = new(2026, 8, 20);

    [Test]
    public void Post_Buy_IncreasesQuantityAndCostBasis_AndRaisesEvents()
    {
        var (account, holding) = OpenWithHolding(100m, 18500m);
        account.ClearDomainEvents();

        var transaction = account.Post(
            TransactionType.Buy,
            BookedOn,
            Money.Of(9250m, "NZD"),
            holding.Id,
            50m,
            "  Top-up  ");

        holding.Quantity.ShouldBe(150m);
        holding.CostBasis.ShouldBe(Money.Of(27750m, "NZD"));
        transaction.TenantId.ShouldBe(account.TenantId);
        transaction.AccountId.ShouldBe(account.Id);
        transaction.HoldingId.ShouldBe(holding.Id);
        transaction.Type.ShouldBe(TransactionType.Buy);
        transaction.BookedOn.ShouldBe(BookedOn);
        transaction.Amount.ShouldBe(Money.Of(9250m, "NZD"));
        transaction.Quantity.ShouldBe(50m);
        transaction.Note.ShouldBe("Top-up");
        account.Transactions.ShouldContain(transaction);
        account.DomainEvents.OfType<TransactionPostedEvent>().Single().Transaction.ShouldBe(transaction);
        account.DomainEvents.OfType<HoldingChangedEvent>().Single().Holding.ShouldBe(holding);
    }

    [Test]
    public void Post_Sell_ReducesQuantityAndCostBasisProportionally()
    {
        var (account, holding) = OpenWithHolding(100m, 18500m);
        account.ClearDomainEvents();

        var transaction = account.Post(
            TransactionType.Sell,
            BookedOn,
            Money.Of(7400m, "NZD"),
            holding.Id,
            40m,
            null);

        holding.Quantity.ShouldBe(60m);
        holding.CostBasis.ShouldBe(Money.Of(11100m, "NZD"));
        transaction.Type.ShouldBe(TransactionType.Sell);
        transaction.Note.ShouldBeNull();
        account.DomainEvents.OfType<TransactionPostedEvent>().ShouldNotBeEmpty();
        account.DomainEvents.OfType<HoldingChangedEvent>().ShouldNotBeEmpty();
    }

    [Test]
    public void Post_SellAll_ZerosQuantityAndCostBasis()
    {
        var (account, holding) = OpenWithHolding(100m, 18500m);

        account.Post(TransactionType.Sell, BookedOn, Money.Of(18500m, "NZD"), holding.Id, 100m, null);

        holding.Quantity.ShouldBe(0m);
        holding.CostBasis.ShouldBe(Money.Of(0m, "NZD"));
        account.Holdings.ShouldContain(holding);
    }

    [Test]
    public void Post_Sell_QuantityExceedingHolding_IsRejected()
    {
        var (account, holding) = OpenWithHolding(100m, 18500m);
        account.ClearDomainEvents();

        var action = () => account.Post(
            TransactionType.Sell,
            BookedOn,
            Money.Of(1m, "NZD"),
            holding.Id,
            101m,
            null);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("quantity");
        holding.Quantity.ShouldBe(100m);
        holding.CostBasis.Amount.ShouldBe(18500m);
        account.Transactions.ShouldBeEmpty();
        account.DomainEvents.ShouldBeEmpty();
    }

    [Test]
    public void Post_WhenClosed_Throws()
    {
        var (account, holding) = OpenWithHolding(100m, 18500m);
        account.Close();
        account.ClearDomainEvents();

        var action = () => account.Post(
            TransactionType.Buy,
            BookedOn,
            Money.Of(1m, "NZD"),
            holding.Id,
            1m,
            null);

        Should.Throw<InvalidOperationException>(action).Message.ShouldContain("Closed accounts reject writes");
        holding.Quantity.ShouldBe(100m);
        account.Transactions.ShouldBeEmpty();
        account.DomainEvents.ShouldBeEmpty();
    }

    [TestCase(TransactionType.TransferIn)]
    [TestCase(TransactionType.TransferOut)]
    [TestCase(TransactionType.Dividend)]
    [TestCase(TransactionType.Interest)]
    public void Post_CashType_LeavesHoldingsUntouched_AndRaisesPostedEvent(TransactionType type)
    {
        var (account, holding) = OpenWithHolding(100m, 18500m);
        account.ClearDomainEvents();

        var transaction = account.Post(type, BookedOn, Money.Of(120.50m, "NZD"), null, null, "Q2");

        holding.Quantity.ShouldBe(100m);
        holding.CostBasis.Amount.ShouldBe(18500m);
        transaction.HoldingId.ShouldBeNull();
        transaction.Quantity.ShouldBeNull();
        transaction.Type.ShouldBe(type);
        account.DomainEvents.OfType<TransactionPostedEvent>().Single().Transaction.ShouldBe(transaction);
        account.DomainEvents.OfType<HoldingChangedEvent>().ShouldBeEmpty();
    }

    [TestCase(TransactionType.TransferIn)]
    [TestCase(TransactionType.Dividend)]
    public void Post_CashType_RejectsHoldingId(TransactionType type)
    {
        var (account, holding) = OpenWithHolding(100m, 18500m);

        var action = () => account.Post(type, BookedOn, Money.Of(1m, "NZD"), holding.Id, null, null);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("holdingId");
        account.Transactions.ShouldBeEmpty();
        holding.Quantity.ShouldBe(100m);
    }

    [Test]
    public void Post_CashType_RejectsQuantity()
    {
        var (account, _) = OpenWithHolding(100m, 18500m);

        var action = () => account.Post(
            TransactionType.Interest,
            BookedOn,
            Money.Of(1m, "NZD"),
            null,
            1m,
            null);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("quantity");
        account.Transactions.ShouldBeEmpty();
    }

    [Test]
    public void Post_Buy_WithoutHolding_Throws()
    {
        var account = OpenAccount();

        var action = () => account.Post(TransactionType.Buy, BookedOn, Money.Of(1m, "NZD"), null, 1m, null);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("holdingId");
        account.Transactions.ShouldBeEmpty();
    }

    [Test]
    public void Post_Buy_HoldingOnAnotherAccount_Throws()
    {
        var (account, _) = OpenWithHolding(100m, 18500m);

        var action = () => account.Post(TransactionType.Buy, BookedOn, Money.Of(1m, "NZD"), 99, 1m, null);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("holdingId");
        account.Transactions.ShouldBeEmpty();
    }

    [Test]
    public void Post_RejectsCurrencyMismatch()
    {
        var (account, holding) = OpenWithHolding(100m, 18500m);

        var action = () => account.Post(
            TransactionType.Buy,
            BookedOn,
            Money.Of(1m, "USD"),
            holding.Id,
            1m,
            null);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("amount");
        holding.Quantity.ShouldBe(100m);
        account.Transactions.ShouldBeEmpty();
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void Post_RejectsNonPositiveAmount(decimal amount)
    {
        var (account, holding) = OpenWithHolding(100m, 18500m);

        var action = () => account.Post(
            TransactionType.Buy,
            BookedOn,
            Money.Of(amount, "NZD"),
            holding.Id,
            1m,
            null);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("amount");
        account.Transactions.ShouldBeEmpty();
    }

    [Test]
    public void Post_RejectsUndefinedType()
    {
        var account = OpenAccount();

        var action = () => account.Post(
            (TransactionType)99,
            BookedOn,
            Money.Of(1m, "NZD"),
            null,
            null,
            null);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("type");
    }

    [Test]
    public void Post_TrimsNote_AndRejectsOverMaxLength()
    {
        var account = OpenAccount();
        account.Post(TransactionType.Dividend, BookedOn, Money.Of(1m, "NZD"), null, null, "  ok  ")
            .Note.ShouldBe("ok");

        var tooLong = new string('a', Transaction.NoteMaxLength + 1);
        var action = () => account.Post(TransactionType.Dividend, BookedOn, Money.Of(1m, "NZD"), null, null, tooLong);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("note");
    }

    [Test]
    public void RemoveHolding_WhenHistoricalTransactionsExist_Throws()
    {
        var (account, holding) = OpenWithHolding(100m, 18500m);
        account.Post(TransactionType.Buy, BookedOn, Money.Of(1m, "NZD"), holding.Id, 1m, null);

        var action = () => { account.RemoveHolding(holding.Id); };

        Should.Throw<InvalidOperationException>(action)
            .Message.ShouldContain("historical transactions");
        account.Holdings.ShouldContain(holding);
    }

    [Test]
    public void RemoveHolding_WhenNoTransactions_Succeeds()
    {
        var (account, holding) = OpenWithHolding(100m, 18500m);

        account.RemoveHolding(holding.Id).ShouldBeTrue();

        account.Holdings.ShouldBeEmpty();
    }

    private static Account OpenAccount() => Account.Open(1, 42, "Primary Brokerage", AccountType.Brokerage, "NZD");

    private static (Account Account, Holding Holding) OpenWithHolding(decimal quantity, decimal cost)
    {
        var account = OpenAccount();
        var holding = account.AddHolding(Instrument.Create("Apple Inc.", "AAPL"), quantity, Money.Of(cost, "NZD"));
        holding.Id = 5;
        return (account, holding);
    }
}
