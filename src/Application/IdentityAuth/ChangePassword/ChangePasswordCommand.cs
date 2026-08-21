using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;

namespace MyWealth.Application.IdentityAuth.ChangePassword;

[Authorize]
public record ChangePasswordCommand : IRequest
{
    public string? CurrentPassword { get; init; }

    public string? NewPassword { get; init; }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public ChangePasswordCommandHandler(IUser user, IIdentityService identityService)
    {
        _user = user;
        _identityService = identityService;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is null)
        {
            throw new UnauthorizedAccessException();
        }

        var result = await _identityService.ChangePasswordAsync(
            _user.Id,
            request.CurrentPassword!,
            request.NewPassword!,
            cancellationToken);

        if (!result.Succeeded)
        {
            throw new MyWealth.Application.Common.Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["NewPassword"] = result.Errors
            });
        }
    }
}
