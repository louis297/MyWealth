using MyWealth.Application.Holdings;
using MyWealth.Application.Transactions.CreateTransaction;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Transactions;

public class CreateTransactionCommandValidatorTests
{
    private CreateTransactionCommandValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new CreateTransactionCommandValidator();

    [Test]
    public async Task ShouldRequireAccountBookedOnTypeAndAmount()
    {
        var result = await _validator.ValidateAsync(new CreateTransactionCommand());

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "AccountId");
        result.Errors.ShouldContain(e => e.PropertyName == "BookedOn");
        result.Errors.ShouldContain(e => e.PropertyName == "Type");
        result.Errors.ShouldContain(e => e.PropertyName == "Amount");
    }

    [Test]
    public async Task Buy_RequiresHoldingIdAndQuantity()
    {
        var result = await _validator.ValidateAsync(ValidBuy() with { HoldingId = null, Quantity = null });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "HoldingId");
        result.Errors.ShouldContain(e => e.PropertyName == "Quantity");
    }

    [Test]
    public async Task Buy_RejectsZeroQuantity()
    {
        var result = await _validator.ValidateAsync(ValidBuy() with { Quantity = 0m });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Quantity");
    }

    [Test]
    public async Task Cash_RejectsHoldingIdAndQuantity()
    {
        var result = await _validator.ValidateAsync(ValidDividend() with { HoldingId = 5, Quantity = 1m });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "HoldingId");
        result.Errors.ShouldContain(e => e.PropertyName == "Quantity");
    }

    [Test]
    public async Task ShouldRejectNonPositiveAmount()
    {
        var result = await _validator.ValidateAsync(ValidBuy() with
        {
            Amount = new MoneyDto { Amount = 0m, Currency = "NZD" }
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Amount.Amount");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("NZ")]
    [TestCase("NZDD")]
    [TestCase("N1D")]
    public async Task ShouldRejectInvalidCurrency(string? currency)
    {
        var result = await _validator.ValidateAsync(ValidBuy() with
        {
            Amount = new MoneyDto { Amount = 1m, Currency = currency }
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Amount.Currency");
    }

    [Test]
    public async Task ShouldRejectNoteLongerThanMaxLength()
    {
        var result = await _validator.ValidateAsync(ValidDividend() with
        {
            Note = new string('a', Transaction.NoteMaxLength + 1)
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Note");
    }

    [Test]
    public async Task ShouldAcceptValidBuyAndCash()
    {
        (await _validator.ValidateAsync(ValidBuy())).IsValid.ShouldBeTrue();
        (await _validator.ValidateAsync(ValidDividend())).IsValid.ShouldBeTrue();
    }

    private static CreateTransactionCommand ValidBuy() => new()
    {
        AccountId = 1,
        HoldingId = 5,
        BookedOn = new DateOnly(2026, 8, 20),
        Type = TransactionType.Buy,
        Amount = new MoneyDto { Amount = 18500m, Currency = "NZD" },
        Quantity = 100m,
        Note = "Initial purchase"
    };

    private static CreateTransactionCommand ValidDividend() => new()
    {
        AccountId = 1,
        BookedOn = new DateOnly(2026, 8, 20),
        Type = TransactionType.Dividend,
        Amount = new MoneyDto { Amount = 120.50m, Currency = "NZD" },
        Note = "Q2 dividend"
    };
}
