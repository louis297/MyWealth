using MyWealth.Application.Dashboard;
using MyWealth.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Dashboard;

public class DashboardCalculatorTests
{
    [TestCase(TransactionType.TransferIn, 10, 10)]
    [TestCase(TransactionType.Dividend, 10, 10)]
    [TestCase(TransactionType.Interest, 10, 10)]
    [TestCase(TransactionType.Sell, 10, 10)]
    [TestCase(TransactionType.TransferOut, 10, -10)]
    [TestCase(TransactionType.Buy, 10, -10)]
    public void SignedContribution_UsesLockedSignConvention(TransactionType type, decimal amount, decimal expected)
    {
        DashboardCalculator.SignedContribution(type, amount).ShouldBe(expected);
    }

    [Test]
    public void AccountValue_BankUsesSignedSumAndIgnoresHoldings()
    {
        var txs = new (TransactionType Type, decimal Amount)[]
        {
            (TransactionType.TransferIn, 250m),
            (TransactionType.Buy, 100m),
            (TransactionType.Sell, 20m),
            (TransactionType.TransferOut, 30m),
            (TransactionType.Dividend, 5m),
            (TransactionType.Interest, 2m)
        };

        var value = DashboardCalculator.AccountValue(AccountType.Bank, txs, [999m]);

        value.ShouldBe(147m);
    }

    [Test]
    public void AccountValue_CashAndOtherMatchBank()
    {
        var txs = new (TransactionType Type, decimal Amount)[] { (TransactionType.TransferIn, 80m) };

        DashboardCalculator.AccountValue(AccountType.Cash, txs, [10m]).ShouldBe(80m);
        DashboardCalculator.AccountValue(AccountType.Other, txs, [10m]).ShouldBe(80m);
    }

    [Test]
    public void AccountValue_CreditUsesSignedSum()
    {
        var txs = new (TransactionType Type, decimal Amount)[]
        {
            (TransactionType.TransferIn, 500m),
            (TransactionType.TransferOut, 50m)
        };

        DashboardCalculator.AccountValue(AccountType.Credit, txs, []).ShouldBe(450m);
    }

    [Test]
    public void AccountValue_BrokerageAndPropertyUseCostBasisAndIgnoreTransactions()
    {
        var txs = new (TransactionType Type, decimal Amount)[] { (TransactionType.Buy, 1000m) };

        DashboardCalculator.AccountValue(AccountType.Brokerage, txs, [50m, 25m]).ShouldBe(75m);
        DashboardCalculator.AccountValue(AccountType.Property, txs, [40m]).ShouldBe(40m);
    }

    [Test]
    public void AccountValue_EmptySourcesAreZero()
    {
        DashboardCalculator.AccountValue(AccountType.Bank, [], []).ShouldBe(0m);
        DashboardCalculator.AccountValue(AccountType.Brokerage, [], []).ShouldBe(0m);
    }

    [Test]
    public void ToNetWorth_EmptyInput_ReturnsEmptyItems()
    {
        var result = DashboardCalculator.ToNetWorth([]);

        result.Items.ShouldBeEmpty();
    }

    [Test]
    public void ToNetWorth_GroupsByCurrency_TreatsCreditAsLiability()
    {
        var result = DashboardCalculator.ToNetWorth(
        [
            new AccountContribution(AccountType.Bank, "NZD", 100m),
            new AccountContribution(AccountType.Brokerage, "NZD", 200m),
            new AccountContribution(AccountType.Credit, "NZD", 40m),
            new AccountContribution(AccountType.Cash, "USD", 20m)
        ]);

        result.Items.Count.ShouldBe(2);

        var nzd = result.Items[0];
        nzd.Currency.ShouldBe("NZD");
        nzd.Assets.ShouldBe(300m);
        nzd.Liabilities.ShouldBe(40m);
        nzd.Net.ShouldBe(260m);

        var usd = result.Items[1];
        usd.Currency.ShouldBe("USD");
        usd.Assets.ShouldBe(20m);
        usd.Liabilities.ShouldBe(0m);
        usd.Net.ShouldBe(20m);
    }

    [Test]
    public void ToAllocation_EmptyInput_ReturnsEmptyItems()
    {
        var result = DashboardCalculator.ToAllocation([]);

        result.Items.ShouldBeEmpty();
    }

    [Test]
    public void ToAllocation_GroupsByAccountTypeAndCurrency()
    {
        var result = DashboardCalculator.ToAllocation(
        [
            new AccountContribution(AccountType.Bank, "NZD", 10m),
            new AccountContribution(AccountType.Bank, "NZD", 15m),
            new AccountContribution(AccountType.Credit, "NZD", 40m),
            new AccountContribution(AccountType.Brokerage, "USD", 5m)
        ]);

        result.Items.Count.ShouldBe(3);
        result.Items[0].AccountType.ShouldBe(AccountType.Bank);
        result.Items[0].Currency.ShouldBe("NZD");
        result.Items[0].Value.ShouldBe(25m);
        result.Items[1].AccountType.ShouldBe(AccountType.Brokerage);
        result.Items[1].Currency.ShouldBe("USD");
        result.Items[1].Value.ShouldBe(5m);
        result.Items[2].AccountType.ShouldBe(AccountType.Credit);
        result.Items[2].Currency.ShouldBe("NZD");
        result.Items[2].Value.ShouldBe(40m);
    }
}
