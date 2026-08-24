using MyWealth.Application.Accounts;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.Transactions.GetTransactions;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public class GetTransactionsQuery : IRequest<PaginatedList<TransactionVm>>
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public int? AccountId { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public TransactionType? Type { get; init; }
}

public class GetTransactionsQueryHandler : IRequestHandler<GetTransactionsQuery, PaginatedList<TransactionVm>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetTransactionsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<PaginatedList<TransactionVm>> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var adviserDomainUserId = AccountVisibility.IsAdviser(_user)
            ? await AccountVisibility.GetCallerDomainUserIdAsync(_context, _user, cancellationToken)
            : null;

        var accounts = AccountVisibility.ScopedAccounts(
            _context.Accounts.AsNoTracking(),
            _context.Users.AsNoTracking(),
            _user,
            adviserDomainUserId);

        var transactions = from tx in _context.Transactions.AsNoTracking()
                           join account in accounts on tx.AccountId equals account.Id
                           select tx;

        if (request.AccountId is int accountId)
        {
            transactions = transactions.Where(t => t.AccountId == accountId);
        }

        if (request.From is DateOnly from)
        {
            transactions = transactions.Where(t => t.BookedOn >= from);
        }

        if (request.To is DateOnly to)
        {
            transactions = transactions.Where(t => t.BookedOn <= to);
        }

        if (request.Type is TransactionType type)
        {
            transactions = transactions.Where(t => t.Type == type);
        }

        var query = TransactionMappings.ProjectToVm(transactions)
            .OrderByDescending(t => t.BookedOn)
            .ThenByDescending(t => t.Id);

        return await PaginatedList<TransactionVm>.CreateAsync(
            query,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
