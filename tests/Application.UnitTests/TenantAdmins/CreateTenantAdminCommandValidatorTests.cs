using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.TenantAdmins.CreateTenantAdmin;
using MyWealth.Domain.Entities;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.TenantAdmins;

public class CreateTenantAdminCommandValidatorTests
{
    private CreateTenantAdminCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        var context = new Mock<IApplicationDbContext>();
        _validator = new CreateTenantAdminCommandValidator(context.Object);
    }

    [Test]
    public async Task ShouldRequireTenantIdNameEmailAndPassword()
    {
        var result = await _validator.ValidateAsync(new CreateTenantAdminCommand());

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "TenantId");
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }

    [Test]
    public async Task ShouldRejectEmptyName()
    {
        var result = await _validator.ValidateAsync(new CreateTenantAdminCommand
        {
            Name = "  ",
            Password = "P@ssw0rd!"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldRejectNameLongerThanMaxLength()
    {
        var result = await _validator.ValidateAsync(new CreateTenantAdminCommand
        {
            Name = new string('a', User.NameMaxLength + 1),
            Password = "P@ssw0rd!"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldRejectEmptyEmail()
    {
        var result = await _validator.ValidateAsync(new CreateTenantAdminCommand
        {
            Name = "Alice",
            Email = "  ",
            Password = "P@ssw0rd!"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Test]
    public async Task ShouldRejectEmptyPassword()
    {
        var result = await _validator.ValidateAsync(new CreateTenantAdminCommand
        {
            Name = "Alice",
            Password = "  "
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }
}
