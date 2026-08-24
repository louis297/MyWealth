using MyWealth.Application.Accounts.CreateAccount;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Accounts;

public class CreateAccountCommandValidatorTests
{
    private CreateAccountCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        var context = new Mock<IApplicationDbContext>();
        var user = new Mock<IUser>();
        user.Setup(u => u.Roles).Returns([Roles.TenantAdmin]);
        _validator = new CreateAccountCommandValidator(context.Object, user.Object);
    }

    [Test]
    public async Task ShouldRequireCustomerNameTypeAndCurrency()
    {
        var result = await _validator.ValidateAsync(new CreateAccountCommand());

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CustomerId");
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
        result.Errors.ShouldContain(e => e.PropertyName == "Type");
        result.Errors.ShouldContain(e => e.PropertyName == "Currency");
    }

    [Test]
    public async Task ShouldRejectEmptyName()
    {
        var result = await _validator.ValidateAsync(new CreateAccountCommand
        {
            Name = "  ",
            Type = AccountType.Cash,
            Currency = "NZD"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldRejectNameLongerThanMaxLength()
    {
        var result = await _validator.ValidateAsync(new CreateAccountCommand
        {
            Name = new string('a', Account.NameMaxLength + 1),
            Type = AccountType.Cash,
            Currency = "NZD"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldRejectUndefinedType()
    {
        var result = await _validator.ValidateAsync(new CreateAccountCommand
        {
            Name = "Cash",
            Type = (AccountType)99,
            Currency = "NZD"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Type");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("NZ")]
    [TestCase("NZDD")]
    [TestCase("N1D")]
    public async Task ShouldRejectInvalidCurrency(string? currency)
    {
        var result = await _validator.ValidateAsync(new CreateAccountCommand
        {
            Name = "Cash",
            Type = AccountType.Cash,
            Currency = currency
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Currency");
    }

    [Test]
    public async Task ShouldRejectMissingCustomerId()
    {
        var result = await _validator.ValidateAsync(new CreateAccountCommand
        {
            Name = "Cash",
            Type = AccountType.Cash,
            Currency = "NZD"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CustomerId");
    }
}
