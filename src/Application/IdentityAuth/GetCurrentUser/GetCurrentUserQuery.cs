using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;

namespace MyWealth.Application.IdentityAuth.GetCurrentUser;

[Authorize]
public record GetCurrentUserQuery : IRequest<CurrentUserVm>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserVm>
{
    private readonly IUser _user;
    private readonly IIdentityService _identityService;
    private readonly IApplicationDbContext _context;

    public GetCurrentUserQueryHandler(IUser user, IIdentityService identityService, IApplicationDbContext context)
    {
        _user = user;
        _identityService = identityService;
        _context = context;
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

        var domainUserId = await _context.Users
            .AsNoTracking()
            .Where(u => u.IdentityUserId == _user.Id)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new CurrentUserVm
        {
            UserId = currentUser.UserId,
            Email = currentUser.Email,
            DisplayName = currentUser.DisplayName,
            Role = currentUser.Role,
            TenantId = currentUser.TenantId,
            IsEnabled = currentUser.IsEnabled,
            DomainUserId = domainUserId
        };
    }
}
