using MyWealth.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Domain.UnitTests.ValueObjects;

public class MoneyTests
{
    [Test]
    public void Of_NormalisesCurrency()
    {
        var money = Money.Of(18500.00m, " nzd ");

        money.Amount.ShouldBe(18500.00m);
        money.Currency.ShouldBe("NZD");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("NZ")]
    [TestCase("NZDD")]
    [TestCase("N1D")]
    [TestCase("12D")]
    public void Of_RejectsInvalidCurrency(string? currency)
    {
        var action = () => Money.Of(1m, currency!);

        Should.Throw<ArgumentException>(action).ParamName.ShouldBe("currency");
    }

    [Test]
    public void WithAmount_KeepsCurrency()
    {
        var money = Money.Of(100m, "NZD").WithAmount(150m);

        money.Amount.ShouldBe(150m);
        money.Currency.ShouldBe("NZD");
    }

    [Test]
    public void Equality_ComparesAmountAndCurrency()
    {
        Money.Of(10m, "NZD").ShouldBe(Money.Of(10m, "nzd"));
        Money.Of(10m, "NZD").ShouldNotBe(Money.Of(11m, "NZD"));
        Money.Of(10m, "NZD").ShouldNotBe(Money.Of(10m, "USD"));
    }
}
