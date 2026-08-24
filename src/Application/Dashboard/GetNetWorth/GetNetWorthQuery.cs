using MyWealth.Application.Accounts;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;

namespace MyWealth.Application.Dashboard.GetNetWorth;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public record GetNetWorthQuery : IRequest<NetWorthVm>
{
    public int? CustomerId { get; init; }
}

public class GetNetWorthQueryHandler : IRequestHandler<GetNetWorthQuery, NetWorthVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetNetWorthQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<NetWorthVm> Handle(GetNetWorthQuery request, CancellationToken cancellationToken)
    {
        var contributions = await DashboardScope.LoadContributionsAsync(
            _context, _user, request.CustomerId, cancellationToken);

        return DashboardCalculator.ToNetWorth(contributions);
    }
}
