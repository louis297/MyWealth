using MyWealth.Application.Advisers.GetAdvisers;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Advisers;

public class GetAdvisersQueryValidatorTests
{
    private GetAdvisersQueryValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new GetAdvisersQueryValidator();

    [Test]
    public void ShouldRejectPageBelowOne()
    {
        var result = _validator.Validate(new GetAdvisersQuery { Page = 0 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Page");
    }

    [Test]
    public void ShouldRejectPageSizeOutOfRange()
    {
        _validator.Validate(new GetAdvisersQuery { PageSize = 0 }).IsValid.ShouldBeFalse();
        _validator.Validate(new GetAdvisersQuery { PageSize = 101 }).IsValid.ShouldBeFalse();
    }

    [Test]
    public void ShouldAcceptDefaults()
    {
        var result = _validator.Validate(new GetAdvisersQuery());

        result.IsValid.ShouldBeTrue();
    }
}
