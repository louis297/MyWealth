using MyWealth.Application.Dashboard.GetNetWorth;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Dashboard;

public class GetNetWorthQueryValidatorTests
{
    private GetNetWorthQueryValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new GetNetWorthQueryValidator();

    [Test]
    public void ShouldAcceptOmittedCustomerId()
    {
        var result = _validator.Validate(new GetNetWorthQuery());

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldAcceptPositiveCustomerId()
    {
        var result = _validator.Validate(new GetNetWorthQuery { CustomerId = 1 });

        result.IsValid.ShouldBeTrue();
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void ShouldRejectNonPositiveCustomerId(int customerId)
    {
        var result = _validator.Validate(new GetNetWorthQuery { CustomerId = customerId });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CustomerId");
    }
}
