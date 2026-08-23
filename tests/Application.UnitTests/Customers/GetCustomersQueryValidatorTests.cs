using MyWealth.Application.Customers.GetCustomers;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Customers;

public class GetCustomersQueryValidatorTests
{
    private GetCustomersQueryValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new GetCustomersQueryValidator();

    [Test]
    public void ShouldRejectPageBelowOne()
    {
        var result = _validator.Validate(new GetCustomersQuery { Page = 0 });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Page");
    }

    [Test]
    public void ShouldRejectPageSizeOutOfRange()
    {
        _validator.Validate(new GetCustomersQuery { PageSize = 0 }).IsValid.ShouldBeFalse();
        _validator.Validate(new GetCustomersQuery { PageSize = 101 }).IsValid.ShouldBeFalse();
    }

    [Test]
    public void ShouldAcceptDefaults()
    {
        var result = _validator.Validate(new GetCustomersQuery());

        result.IsValid.ShouldBeTrue();
    }
}
