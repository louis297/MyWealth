using MyWealth.Application.Transactions.GetTransactions;
using MyWealth.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Transactions;

public class GetTransactionsQueryValidatorTests
{
    private GetTransactionsQueryValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new GetTransactionsQueryValidator();

    [Test]
    public async Task ShouldRejectPageBelowOne()
    {
        var result = await _validator.ValidateAsync(new GetTransactionsQuery { Page = 0 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Page");
    }

    [Test]
    public async Task ShouldRejectPageSizeOutOfRange()
    {
        var tooSmall = await _validator.ValidateAsync(new GetTransactionsQuery { PageSize = 0 });
        var tooLarge = await _validator.ValidateAsync(new GetTransactionsQuery { PageSize = 101 });

        tooSmall.IsValid.ShouldBeFalse();
        tooLarge.IsValid.ShouldBeFalse();
        tooSmall.Errors.ShouldContain(e => e.PropertyName == "PageSize");
        tooLarge.Errors.ShouldContain(e => e.PropertyName == "PageSize");
    }

    [Test]
    public async Task ShouldRejectNonPositiveAccountId()
    {
        var result = await _validator.ValidateAsync(new GetTransactionsQuery { AccountId = 0 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "AccountId");
    }

    [Test]
    public async Task ShouldRejectUndefinedType()
    {
        var result = await _validator.ValidateAsync(new GetTransactionsQuery { Type = (TransactionType)99 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Type");
    }

    [Test]
    public async Task ShouldRejectFromAfterTo()
    {
        var result = await _validator.ValidateAsync(new GetTransactionsQuery
        {
            From = new DateOnly(2026, 8, 21),
            To = new DateOnly(2026, 8, 20)
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "From");
    }

    [Test]
    public async Task ShouldAcceptDefaults()
    {
        var result = await _validator.ValidateAsync(new GetTransactionsQuery());

        result.IsValid.ShouldBeTrue();
    }
}
