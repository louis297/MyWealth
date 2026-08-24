using MyWealth.Application.Holdings;
using MyWealth.Application.Holdings.UpdateHolding;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Holdings;

public class UpdateHoldingCommandValidatorTests
{
    private UpdateHoldingCommandValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateHoldingCommandValidator();

    [Test]
    public void ShouldRequireAtLeastOneUpdatableField()
    {
        var result = _validator.Validate(new UpdateHoldingCommand { AccountId = 1, Id = 1 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("At least one of instrument, quantity or costBasis.amount"));
    }

    [Test]
    public void ShouldRejectCostBasisWithoutAmountWhenNothingElseSupplied()
    {
        var result = _validator.Validate(new UpdateHoldingCommand
        {
            AccountId = 1,
            Id = 1,
            CostBasis = new MoneyDto()
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("At least one of instrument, quantity or costBasis.amount"));
    }

    [Test]
    public void ShouldRejectEmptyInstrumentNameWhenSupplied()
    {
        var result = _validator.Validate(new UpdateHoldingCommand
        {
            AccountId = 1,
            Id = 1,
            Instrument = new InstrumentDto { Name = "  " }
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Instrument.Name");
    }

    [Test]
    public void ShouldRejectNegativeQuantity()
    {
        var result = _validator.Validate(new UpdateHoldingCommand { AccountId = 1, Id = 1, Quantity = -1m });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Quantity");
    }

    [Test]
    public void ShouldAllowQuantityOnly()
    {
        var result = _validator.Validate(new UpdateHoldingCommand { AccountId = 1, Id = 1, Quantity = 0m });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldRejectCurrencyWhenSupplied()
    {
        var result = _validator.Validate(new UpdateHoldingCommand
        {
            AccountId = 1,
            Id = 1,
            CostBasis = new MoneyDto { Amount = 100m, Currency = "NZD" }
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CostBasis.Currency");
    }

    [Test]
    public void ShouldAllowAmountOnly()
    {
        var result = _validator.Validate(new UpdateHoldingCommand
        {
            AccountId = 1,
            Id = 1,
            CostBasis = new MoneyDto { Amount = 100m }
        });

        result.IsValid.ShouldBeTrue();
    }
}
