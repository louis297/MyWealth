using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Tenants.UpdateTenant;
using MyWealth.Domain.Entities;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Tenants;

public class UpdateTenantCommandValidatorTests
{
    private UpdateTenantCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        var context = new Mock<IApplicationDbContext>();
        _validator = new UpdateTenantCommandValidator(context.Object);
    }

    [Test]
    public async Task ShouldRequireAtLeastOneField()
    {
        var result = await _validator.ValidateAsync(new UpdateTenantCommand { Id = 1 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("At least one of Name or IsEnabled"));
    }

    [Test]
    public async Task ShouldRejectEmptyNameWhenSupplied()
    {
        var result = await _validator.ValidateAsync(new UpdateTenantCommand { Id = 1, Name = "  " });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldRejectNameLongerThanMaxLength()
    {
        var result = await _validator.ValidateAsync(new UpdateTenantCommand
        {
            Id = 1,
            Name = new string('a', Tenant.NameMaxLength + 1)
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldAllowIsEnabledOnly()
    {
        var result = await _validator.ValidateAsync(new UpdateTenantCommand { Id = 1, IsEnabled = false });

        result.IsValid.ShouldBeTrue();
    }
}
