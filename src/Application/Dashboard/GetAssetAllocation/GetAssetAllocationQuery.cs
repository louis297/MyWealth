using MyWealth.Application.Accounts;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;

namespace MyWealth.Application.Dashboard.GetAssetAllocation;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public record GetAssetAllocationQuery : IRequest<AssetAllocationVm>
{
    public int? CustomerId { get; init; }
}

public class GetAssetAllocationQueryHandler : IRequestHandler<GetAssetAllocationQuery, AssetAllocationVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetAssetAllocationQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<AssetAllocationVm> Handle(GetAssetAllocationQuery request, CancellationToken cancellationToken)
    {
        var contributions = await DashboardScope.LoadContributionsAsync(
            _context, _user, request.CustomerId, cancellationToken);

        return DashboardCalculator.ToAllocation(contributions);
    }
}
