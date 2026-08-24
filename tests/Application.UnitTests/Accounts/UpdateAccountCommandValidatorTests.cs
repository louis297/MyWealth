using MyWealth.Application.Accounts.UpdateAccount;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Accounts;

public class UpdateAccountCommandValidatorTests
{
    private UpdateAccountCommandValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateAccountCommandValidator();

    [Test]
    public void ShouldRequireAtLeastOneUpdatableField()
    {
        var result = _validator.Validate(new UpdateAccountCommand { Id = 1 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("At least one of Name or Type"));
    }

    [Test]
    public void ShouldRejectEmptyNameWhenSupplied()
    {
        var result = _validator.Validate(new UpdateAccountCommand { Id = 1, Name = "  " });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public void ShouldRejectNameLongerThanMaxLength()
    {
        var result = _validator.Validate(new UpdateAccountCommand
        {
            Id = 1,
            Name = new string('a', Account.NameMaxLength + 1)
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public void ShouldAllowTypeOnly()
    {
        var result = _validator.Validate(new UpdateAccountCommand { Id = 1, Type = AccountType.Bank });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldRejectCurrencyWhenSupplied()
    {
        var result = _validator.Validate(new UpdateAccountCommand
        {
            Id = 1,
            Name = "Cash",
            Currency = "NZD"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Currency");
    }

    [Test]
    public void ShouldRejectCustomerIdWhenSupplied()
    {
        var result = _validator.Validate(new UpdateAccountCommand
        {
            Id = 1,
            Name = "Cash",
            CustomerId = 42
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CustomerId");
    }

    [Test]
    public void ShouldRejectStatusWhenSupplied()
    {
        var result = _validator.Validate(new UpdateAccountCommand
        {
            Id = 1,
            Name = "Cash",
            Status = AccountStatus.Closed
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Status");
    }

    [Test]
    public void ShouldRejectUndefinedType()
    {
        var result = _validator.Validate(new UpdateAccountCommand
        {
            Id = 1,
            Type = (AccountType)99
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Type");
    }
}
