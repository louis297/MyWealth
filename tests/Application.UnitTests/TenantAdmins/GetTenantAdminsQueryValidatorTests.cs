using MyWealth.Application.TenantAdmins.GetTenantAdmins;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.TenantAdmins;

public class GetTenantAdminsQueryValidatorTests
{
    private GetTenantAdminsQueryValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new GetTenantAdminsQueryValidator();

    [Test]
    public void ShouldRejectPageBelowOne()
    {
        var result = _validator.Validate(new GetTenantAdminsQuery { Page = 0 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Page");
    }

    [Test]
    public void ShouldRejectPageSizeOutOfRange()
    {
        _validator.Validate(new GetTenantAdminsQuery { PageSize = 0 }).IsValid.ShouldBeFalse();
        _validator.Validate(new GetTenantAdminsQuery { PageSize = 101 }).IsValid.ShouldBeFalse();
    }

    [Test]
    public void ShouldRejectNonPositiveTenantIdWhenSupplied()
    {
        var result = _validator.Validate(new GetTenantAdminsQuery { TenantId = 0 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "TenantId");
    }

    [Test]
    public void ShouldAcceptDefaults()
    {
        var result = _validator.Validate(new GetTenantAdminsQuery());

        result.IsValid.ShouldBeTrue();
    }
}
