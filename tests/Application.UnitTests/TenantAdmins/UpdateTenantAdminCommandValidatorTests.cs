using MyWealth.Application.TenantAdmins.UpdateTenantAdmin;
using MyWealth.Domain.Entities;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.TenantAdmins;

public class UpdateTenantAdminCommandValidatorTests
{
    private UpdateTenantAdminCommandValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateTenantAdminCommandValidator();

    [Test]
    public async Task ShouldRequireAtLeastOneField()
    {
        var result = await _validator.ValidateAsync(new UpdateTenantAdminCommand { Id = 1 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("At least one of Name or IsEnabled"));
    }

    [Test]
    public async Task ShouldRejectEmptyNameWhenSupplied()
    {
        var result = await _validator.ValidateAsync(new UpdateTenantAdminCommand { Id = 1, Name = "  " });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldRejectNameLongerThanMaxLength()
    {
        var result = await _validator.ValidateAsync(new UpdateTenantAdminCommand
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
        var result = await _validator.ValidateAsync(new UpdateTenantAdminCommand { Id = 1, IsEnabled = false });

        result.IsValid.ShouldBeTrue();
    }
}
