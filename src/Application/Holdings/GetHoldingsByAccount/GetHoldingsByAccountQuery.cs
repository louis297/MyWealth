using MyWealth.Application.Accounts;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;

namespace MyWealth.Application.Holdings.GetHoldingsByAccount;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public record GetHoldingsByAccountQuery : IRequest<List<HoldingVm>>
{
    public int AccountId { get; init; }
}

public class GetHoldingsByAccountQueryHandler : IRequestHandler<GetHoldingsByAccountQuery, List<HoldingVm>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetHoldingsByAccountQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<List<HoldingVm>> Handle(GetHoldingsByAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await AccountVisibility.FindVisibleAccountAsync(
            _context, _user, request.AccountId, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(nameof(Account), request.AccountId);
        }

        return await HoldingMappings
            .ProjectToVm(_context.Holdings.AsNoTracking().Where(h => h.AccountId == request.AccountId))
            .OrderBy(h => h.Instrument.Name)
            .ThenBy(h => h.Id)
            .ToListAsync(cancellationToken);
    }
}
