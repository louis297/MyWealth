using MyWealth.Application.IdentityAuth.UpdateCurrentUser;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.IdentityAuth;

public class UpdateCurrentUserCommandValidatorTests
{
    private UpdateCurrentUserCommandValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateCurrentUserCommandValidator();

    [Test]
    public void ShouldRequireDisplayName()
    {
        var result = _validator.Validate(new UpdateCurrentUserCommand());

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "DisplayName");
    }

    [Test]
    public void ShouldRejectDisplayNameLongerThan200()
    {
        var result = _validator.Validate(new UpdateCurrentUserCommand
        {
            DisplayName = new string('a', 201)
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "DisplayName");
    }

    [Test]
    public void ShouldAcceptDisplayName()
    {
        var result = _validator.Validate(new UpdateCurrentUserCommand { DisplayName = "Ada" });

        result.IsValid.ShouldBeTrue();
    }
}
