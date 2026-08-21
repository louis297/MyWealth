using MyWealth.Application.Common.Exceptions;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Models;
using MyWealth.Application.IdentityAuth.Login;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace MyWealth.Application.UnitTests.IdentityAuth;

public class LoginCommandTests
{
    private Mock<IIdentityService> _identityService = null!;
    private LoginCommandHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _identityService = new Mock<IIdentityService>();
        _handler = new LoginCommandHandler(_identityService.Object);
    }

    [Test]
    public async Task ShouldReturnTokenWhenAuthenticationSucceeds()
    {
        _identityService
            .Setup(s => s.AuthenticateAsync("admin@localhost", "Administrator1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationResult.Success(
                "token",
                28800,
                "user-1",
                "admin@localhost",
                "System Admin",
                "SystemAdmin",
                null));

        var result = await _handler.Handle(
            new LoginCommand { Email = "admin@localhost", Password = "Administrator1!" },
            CancellationToken.None);

        result.AccessToken.ShouldBe("token");
        result.TokenType.ShouldBe("Bearer");
        result.ExpiresIn.ShouldBe(28800);
        result.UserId.ShouldBe("user-1");
        result.Role.ShouldBe("SystemAdmin");
        result.TenantId.ShouldBeNull();
    }

    [Test]
    public async Task ShouldRejectCustomerRole()
    {
        _identityService
            .Setup(s => s.AuthenticateAsync("customer@localhost", "Customer1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationResult.Customer());

        var action = () => _handler.Handle(
            new LoginCommand { Email = "customer@localhost", Password = "Customer1!" },
            CancellationToken.None);

        var ex = await Should.ThrowAsync<ForbiddenAccessException>(action);
        ex.Message.ShouldBe("Customer accounts cannot sign in.");
    }

    [Test]
    public async Task ShouldRejectBadCredentials()
    {
        _identityService
            .Setup(s => s.AuthenticateAsync("admin@localhost", "wrong", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationResult.Failed());

        var action = () => _handler.Handle(
            new LoginCommand { Email = "admin@localhost", Password = "wrong" },
            CancellationToken.None);

        await Should.ThrowAsync<UnauthorizedAccessException>(action);
    }

    [Test]
    public async Task ShouldRejectDisabledUser()
    {
        _identityService
            .Setup(s => s.AuthenticateAsync("admin@localhost", "Administrator1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationResult.Disabled());

        var action = () => _handler.Handle(
            new LoginCommand { Email = "admin@localhost", Password = "Administrator1!" },
            CancellationToken.None);

        await Should.ThrowAsync<UnauthorizedAccessException>(action);
    }
}
