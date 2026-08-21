using MyWealth.Application.Common.Exceptions;
using MyWealth.Application.Common.Interfaces;

namespace MyWealth.Application.IdentityAuth.Login;

public record LoginCommand : IRequest<LoginResultVm>
{
    public string? Email { get; init; }

    public string? Password { get; init; }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultVm>
{
    private readonly IIdentityService _identityService;

    public LoginCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<LoginResultVm> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await _identityService.AuthenticateAsync(
            request.Email!,
            request.Password!,
            cancellationToken);

        if (result.IsCustomer)
        {
            throw new ForbiddenAccessException("Customer accounts cannot sign in.");
        }

        if (!result.Succeeded || result.AccessToken is null)
        {
            throw new UnauthorizedAccessException();
        }

        return new LoginResultVm
        {
            AccessToken = result.AccessToken,
            TokenType = "Bearer",
            ExpiresIn = result.ExpiresIn,
            UserId = result.UserId!,
            Email = result.Email!,
            DisplayName = result.DisplayName!,
            Role = result.Role!,
            TenantId = result.TenantId
        };
    }
}
