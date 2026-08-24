using MyWealth.Application.Accounts.GetAccounts;
using MyWealth.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Accounts;

public class GetAccountsQueryValidatorTests
{
    private GetAccountsQueryValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new GetAccountsQueryValidator();

    [Test]
    public void ShouldRejectPageBelowOne()
    {
        var result = _validator.Validate(new GetAccountsQuery { Page = 0 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Page");
    }

    [Test]
    public void ShouldRejectPageSizeOutOfRange()
    {
        _validator.Validate(new GetAccountsQuery { PageSize = 0 }).IsValid.ShouldBeFalse();
        _validator.Validate(new GetAccountsQuery { PageSize = 101 }).IsValid.ShouldBeFalse();
    }

    [Test]
    public void ShouldAcceptDefaults()
    {
        var result = _validator.Validate(new GetAccountsQuery());

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldRejectUndefinedStatus()
    {
        var result = _validator.Validate(new GetAccountsQuery { Status = (AccountStatus)99 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Status");
    }

    [Test]
    public void ShouldRejectNonPositiveCustomerId()
    {
        var result = _validator.Validate(new GetAccountsQuery { CustomerId = 0 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CustomerId");
    }
}
