using MyWealth.Application.IdentityAuth.ChangePassword;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.IdentityAuth;

public class ChangePasswordCommandValidatorTests
{
    private ChangePasswordCommandValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new ChangePasswordCommandValidator();

    [Test]
    public void ShouldRequireCurrentAndNewPassword()
    {
        var result = _validator.Validate(new ChangePasswordCommand());

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "CurrentPassword");
        result.Errors.ShouldContain(e => e.PropertyName == "NewPassword");
    }

    [Test]
    public void ShouldRejectMatchingPasswords()
    {
        var result = _validator.Validate(new ChangePasswordCommand
        {
            CurrentPassword = "Administrator1!",
            NewPassword = "Administrator1!"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "NewPassword");
    }

    [Test]
    public void ShouldAcceptDifferentPasswords()
    {
        var result = _validator.Validate(new ChangePasswordCommand
        {
            CurrentPassword = "Administrator1!",
            NewPassword = "Administrator2!"
        });

        result.IsValid.ShouldBeTrue();
    }
}
