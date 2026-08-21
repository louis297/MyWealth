using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;

namespace MyWealth.Application.IdentityAuth.GetCurrentUser;

[Authorize]
public record GetCurrentUserQuery : IRequest<CurrentUserVm>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserVm>
{
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public GetCurrentUserQueryHandler(IUser user, IIdentityService identityService)
    {
        _user = user;
        _identityService = identityService;
    }

    public async Task<CurrentUserVm> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_user.Id is null)
        {
            throw new UnauthorizedAccessException();
        }

        var currentUser = await _identityService.GetCurrentUserAsync(_user.Id, cancellationToken);

        if (currentUser is null)
        {
            throw new UnauthorizedAccessException();
        }

        return new CurrentUserVm
        {
            UserId = currentUser.UserId,
            Email = currentUser.Email,
            DisplayName = currentUser.DisplayName,
            Role = currentUser.Role,
            TenantId = currentUser.TenantId,
            IsEnabled = currentUser.IsEnabled
        };
    }
}
