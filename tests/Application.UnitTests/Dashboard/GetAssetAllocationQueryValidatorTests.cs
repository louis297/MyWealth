using MyWealth.Application.Dashboard.GetAssetAllocation;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Dashboard;

public class GetAssetAllocationQueryValidatorTests
{
    private GetAssetAllocationQueryValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new GetAssetAllocationQueryValidator();

    [Test]
    public void ShouldAcceptOmittedCustomerId()
    {
        var result = _validator.Validate(new GetAssetAllocationQuery());

        result.IsValid.ShouldBeTrue();
    }

    [Test]
    public void ShouldAcceptPositiveCustomerId()
    {
        var result = _validator.Validate(new GetAssetAllocationQuery { CustomerId = 1 });

        result.IsValid.ShouldBeTrue();
    }

    [TestCase(0)]
    [TestCase(-1)]
    public void ShouldRejectNonPositiveCustomerId(int customerId)
    {
        var result = _validator.Validate(new GetAssetAllocationQuery { CustomerId = customerId });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CustomerId");
    }
}
