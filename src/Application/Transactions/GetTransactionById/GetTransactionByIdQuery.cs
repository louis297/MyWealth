using MyWealth.Application.Accounts;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;

namespace MyWealth.Application.Transactions.GetTransactionById;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public record GetTransactionByIdQuery : IRequest<TransactionVm>
{
    public int Id { get; init; }
}

public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, TransactionVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetTransactionByIdQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<TransactionVm> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var adviserDomainUserId = AccountVisibility.IsAdviser(_user)
            ? await AccountVisibility.GetCallerDomainUserIdAsync(_context, _user, cancellationToken)
            : null;

        var accounts = AccountVisibility.ScopedAccounts(
            _context.Accounts.AsNoTracking(),
            _context.Users.AsNoTracking(),
            _user,
            adviserDomainUserId);

        var transaction = await TransactionMappings
            .ProjectToVm(
                from tx in _context.Transactions.AsNoTracking()
                join account in accounts on tx.AccountId equals account.Id
                where tx.Id == request.Id
                select tx)
            .FirstOrDefaultAsync(cancellationToken);

        if (transaction is null)
        {
            throw new NotFoundException(nameof(Transaction), request.Id);
        }

        return transaction;
    }
}
