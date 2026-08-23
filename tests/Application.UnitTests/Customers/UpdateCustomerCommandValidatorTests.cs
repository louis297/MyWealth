using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Customers.UpdateCustomer;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Customers;

public class UpdateCustomerCommandValidatorTests
{
    private UpdateCustomerCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        var context = new Mock<IApplicationDbContext>();
        var user = new Mock<IUser>();
        user.Setup(u => u.Roles).Returns([Roles.TenantAdmin]);
        _validator = new UpdateCustomerCommandValidator(context.Object, user.Object);
    }

    [Test]
    public async Task ShouldRequireAtLeastOneField()
    {
        var result = await _validator.ValidateAsync(new UpdateCustomerCommand { Id = 1 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("At least one of Name, IsEnabled or AdviserId"));
    }

    [Test]
    public async Task ShouldRejectEmptyNameWhenSupplied()
    {
        var result = await _validator.ValidateAsync(new UpdateCustomerCommand { Id = 1, Name = "  " });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldRejectNameLongerThanMaxLength()
    {
        var result = await _validator.ValidateAsync(new UpdateCustomerCommand
        {
            Id = 1,
            Name = new string('a', User.NameMaxLength + 1)
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldAllowIsEnabledOnly()
    {
        var result = await _validator.ValidateAsync(new UpdateCustomerCommand { Id = 1, IsEnabled = false });

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public async Task ShouldRejectZeroAdviserIdWhenSupplied()
    {
        var result = await _validator.ValidateAsync(new UpdateCustomerCommand { Id = 1, AdviserId = 0 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "AdviserId");
    }
}
