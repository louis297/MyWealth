using MyWealth.Application.Accounts;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;

namespace MyWealth.Application.Holdings.GetHoldingById;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public record GetHoldingByIdQuery : IRequest<HoldingVm>
{
    public int AccountId { get; init; }

    public int Id { get; init; }
}

public class GetHoldingByIdQueryHandler : IRequestHandler<GetHoldingByIdQuery, HoldingVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetHoldingByIdQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<HoldingVm> Handle(GetHoldingByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await AccountVisibility.FindVisibleAccountAsync(
            _context, _user, request.AccountId, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(nameof(Account), request.AccountId);
        }

        var holding = await HoldingMappings
            .ProjectToVm(_context.Holdings.AsNoTracking()
                .Where(h => h.AccountId == request.AccountId && h.Id == request.Id))
            .FirstOrDefaultAsync(cancellationToken);

        if (holding is null)
        {
            throw new NotFoundException(nameof(Holding), request.Id);
        }

        return holding;
    }
}
