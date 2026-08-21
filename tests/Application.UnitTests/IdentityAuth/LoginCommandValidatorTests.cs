using MyWealth.Application.IdentityAuth.Login;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.IdentityAuth;

public class LoginCommandValidatorTests
{
    private LoginCommandValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new LoginCommandValidator();

    [Test]
    public void ShouldRequireEmailAndPassword()
    {
        var result = _validator.Validate(new LoginCommand());

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }

    [Test]
    public void ShouldAcceptValidEmailAndPassword()
    {
        var result = _validator.Validate(new LoginCommand
        {
            Email = "admin@localhost",
            Password = "Administrator1!"
        });

        result.IsValid.ShouldBeTrue();
    }
}
