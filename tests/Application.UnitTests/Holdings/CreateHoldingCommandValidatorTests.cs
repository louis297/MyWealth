using MyWealth.Application.Holdings;
using MyWealth.Application.Holdings.CreateHolding;
using MyWealth.Domain.ValueObjects;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Holdings;

public class CreateHoldingCommandValidatorTests
{
    private CreateHoldingCommandValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateHoldingCommandValidator();

    [Test]
    public async Task ShouldRequireInstrumentQuantityAndCostBasis()
    {
        var result = await _validator.ValidateAsync(new CreateHoldingCommand { AccountId = 1 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Instrument");
        result.Errors.ShouldContain(e => e.PropertyName == "Quantity");
        result.Errors.ShouldContain(e => e.PropertyName == "CostBasis");
    }

    [Test]
    public async Task ShouldRejectMissingInstrumentName()
    {
        var result = await _validator.ValidateAsync(Valid() with
        {
            Instrument = new InstrumentDto { Symbol = "AAPL" }
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Instrument.Name");
    }

    [Test]
    public async Task ShouldRejectInstrumentNameLongerThanMaxLength()
    {
        var result = await _validator.ValidateAsync(Valid() with
        {
            Instrument = new InstrumentDto { Name = new string('a', Instrument.NameMaxLength + 1) }
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Instrument.Name");
    }

    [Test]
    public async Task ShouldRejectNegativeQuantity()
    {
        var result = await _validator.ValidateAsync(Valid() with { Quantity = -1m });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Quantity");
    }

    [Test]
    public async Task ShouldAllowZeroQuantity()
    {
        var result = await _validator.ValidateAsync(Valid() with { Quantity = 0m });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldRejectNegativeCostBasisAmount()
    {
        var result = await _validator.ValidateAsync(Valid() with
        {
            CostBasis = new MoneyDto { Amount = -1m, Currency = "NZD" }
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CostBasis.Amount");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("NZ")]
    [TestCase("NZDD")]
    [TestCase("N1D")]
    public async Task ShouldRejectInvalidCurrency(string? currency)
    {
        var result = await _validator.ValidateAsync(Valid() with
        {
            CostBasis = new MoneyDto { Amount = 1m, Currency = currency }
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CostBasis.Currency");
    }

    [Test]
    public async Task ShouldRejectMissingAccountId()
    {
        var result = await _validator.ValidateAsync(Valid() with { AccountId = 0 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "AccountId");
    }

    private static CreateHoldingCommand Valid() => new()
    {
        AccountId = 1,
        Instrument = new InstrumentDto { Name = "Apple Inc.", Symbol = "AAPL" },
        Quantity = 100m,
        CostBasis = new MoneyDto { Amount = 18500m, Currency = "NZD" }
    };
}
