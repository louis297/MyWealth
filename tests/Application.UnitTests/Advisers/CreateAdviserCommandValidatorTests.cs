using MyWealth.Application.Advisers.CreateAdviser;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Domain.Entities;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.Advisers;

public class CreateAdviserCommandValidatorTests
{
    private CreateAdviserCommandValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        var context = new Mock<IApplicationDbContext>();
        _validator = new CreateAdviserCommandValidator(context.Object);
    }

    [Test]
    public async Task ShouldRequireNameEmailAndPassword()
    {
        var result = await _validator.ValidateAsync(new CreateAdviserCommand());

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }

    [Test]
    public async Task ShouldRejectEmptyName()
    {
        var result = await _validator.ValidateAsync(new CreateAdviserCommand
        {
            Name = "  ",
            Password = "P@ssw0rd!"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldRejectNameLongerThanMaxLength()
    {
        var result = await _validator.ValidateAsync(new CreateAdviserCommand
        {
            Name = new string('a', User.NameMaxLength + 1),
            Password = "P@ssw0rd!"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Test]
    public async Task ShouldRejectEmptyEmail()
    {
        var result = await _validator.ValidateAsync(new CreateAdviserCommand
        {
            Name = "Jane",
            Email = "  ",
            Password = "P@ssw0rd!"
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email");
    }

    [Test]
    public async Task ShouldRejectEmptyPassword()
    {
        var result = await _validator.ValidateAsync(new CreateAdviserCommand
        {
            Name = "Jane",
            Password = "  "
        });

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password");
    }
}
