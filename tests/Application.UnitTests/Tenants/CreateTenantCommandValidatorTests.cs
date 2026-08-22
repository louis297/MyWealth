using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Tenants.CreateTenant;
using MyWealth.Domain.Entities;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Tenants;

public class CreateTenantCommandValidatorTests
{
    private CreateTenantCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        var context = new Mock<IApplicationDbContext>();
        _validator = new CreateTenantCommandValidator(context.Object);
    }

    [Test]
    public async Task ShouldRequireName()
    {
        var result = await _validator.ValidateAsync(new CreateTenantCommand());

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldRejectEmptyName()
    {
        var result = await _validator.ValidateAsync(new CreateTenantCommand { Name = "  " });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldRejectNameLongerThanMaxLength()
    {
        var result = await _validator.ValidateAsync(new CreateTenantCommand
        {
            Name = new string('a', Tenant.NameMaxLength + 1)
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }
}
