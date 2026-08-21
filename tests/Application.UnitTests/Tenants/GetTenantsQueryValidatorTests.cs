using MyWealth.Application.Tenants.GetTenants;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Tenants;

public class GetTenantsQueryValidatorTests
{
    private GetTenantsQueryValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new GetTenantsQueryValidator();

    [Test]
    public void ShouldRejectPageBelowOne()
    {
        var result = _validator.Validate(new GetTenantsQuery { Page = 0 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Page");
    }

    [Test]
    public void ShouldRejectPageSizeOutOfRange()
    {
        _validator.Validate(new GetTenantsQuery { PageSize = 0 }).IsValid.ShouldBeFalse();
        _validator.Validate(new GetTenantsQuery { PageSize = 101 }).IsValid.ShouldBeFalse();
    }

    [Test]
    public void ShouldAcceptDefaults()
    {
        var result = _validator.Validate(new GetTenantsQuery());

        result.IsValid.ShouldBeTrue();
    }
}
