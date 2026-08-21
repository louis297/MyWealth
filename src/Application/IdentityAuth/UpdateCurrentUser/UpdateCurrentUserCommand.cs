using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;

namespace MyWealth.Application.IdentityAuth.UpdateCurrentUser;

[Authorize]
public record UpdateCurrentUserCommand : IRequest
{
    public string? DisplayName { get; init; }
}

public class UpdateCurrentUserCommandHandler : IRequestHandler<UpdateCurrentUserCommand>
{
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public UpdateCurrentUserCommandHandler(IUser user, IIdentityService identityService)
    {
        _user = user;
        _identityService = identityService;
    }

    public async Task Handle(UpdateCurrentUserCommand request, CancellationToken cancellationToken)
    {
        if (_user.Id is null)
        {
            throw new UnauthorizedAccessException();
        }

        var result = await _identityService.UpdateDisplayNameAsync(
            _user.Id,
            request.DisplayName!,
            cancellationToken);

        if (!result.Succeeded)
        {
            throw new MyWealth.Application.Common.Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["DisplayName"] = result.Errors
            });
        }
    }
}
