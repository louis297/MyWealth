using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Customers.CreateCustomer;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Customers;

public class CreateCustomerCommandValidatorTests
{
    private CreateCustomerCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        var context = new Mock<IApplicationDbContext>();
        var user = new Mock<IUser>();
        user.Setup(u => u.Roles).Returns([Roles.TenantAdmin]);
        _validator = new CreateCustomerCommandValidator(context.Object, user.Object);
    }

    [Test]
    public async Task ShouldRequireNameEmailAndAdviserId()
    {
        var result = await _validator.ValidateAsync(new CreateCustomerCommand());

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
        result.Errors.ShouldContain(e => e.PropertyName == "AdviserId");
    }

    [Test]
    public async Task ShouldRejectEmptyName()
    {
        var result = await _validator.ValidateAsync(new CreateCustomerCommand { Name = "  " });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldRejectNameLongerThanMaxLength()
    {
        var result = await _validator.ValidateAsync(new CreateCustomerCommand
        {
            Name = new string('a', User.NameMaxLength + 1)
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldRejectEmptyEmail()
    {
        var result = await _validator.ValidateAsync(new CreateCustomerCommand
        {
            Name = "Zhang San",
            Email = "  "
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Test]
    public async Task ShouldRejectMissingAdviserId()
    {
        var result = await _validator.ValidateAsync(new CreateCustomerCommand { Name = "Zhang San" });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "AdviserId");
    }
}
