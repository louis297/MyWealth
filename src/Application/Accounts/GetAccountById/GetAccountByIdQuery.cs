using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;

namespace MyWealth.Application.Accounts.GetAccountById;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public record GetAccountByIdQuery : IRequest<AccountVm>
{
    public int Id { get; init; }
}

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetAccountByIdQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<AccountVm> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var adviserDomainUserId = AccountVisibility.IsAdviser(_user)
            ? await AccountVisibility.GetCallerDomainUserIdAsync(_context, _user, cancellationToken)
            : null;

        var accounts = AccountVisibility.ScopedAccounts(
            _context.Accounts.AsNoTracking(),
            _context.Users.AsNoTracking(),
            _user,
            adviserDomainUserId);

        var account = await AccountVisibility
            .ProjectToVm(accounts.Where(a => a.Id == request.Id), _context.Users.AsNoTracking())
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(nameof(Account), request.Id);
        }

        return account;
    }
}
