using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.Accounts.GetAccounts;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public class GetAccountsQuery : IRequest<PaginatedList<AccountVm>>
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public AccountStatus? Status { get; init; }

    public int? CustomerId { get; init; }

    public string? Search { get; init; }
}

public class GetAccountsQueryHandler : IRequestHandler<GetAccountsQuery, PaginatedList<AccountVm>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetAccountsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<PaginatedList<AccountVm>> Handle(GetAccountsQuery request, CancellationToken cancellationToken)
    {
        var adviserDomainUserId = AccountVisibility.IsAdviser(_user)
            ? await AccountVisibility.GetCallerDomainUserIdAsync(_context, _user, cancellationToken)
            : null;

        var accounts = AccountVisibility.ScopedAccounts(
            _context.Accounts.AsNoTracking(),
            _context.Users.AsNoTracking(),
            _user,
            adviserDomainUserId);

        if (request.Status is AccountStatus status)
        {
            accounts = accounts.Where(a => a.Status == status);
        }

        if (request.CustomerId is int customerId)
        {
            accounts = accounts.Where(a => a.CustomerId == customerId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            if (int.TryParse(search, out var id))
            {
                accounts = accounts.Where(a => a.Id == id || a.Name.Contains(search));
            }
            else
            {
                accounts = accounts.Where(a => a.Name.Contains(search));
            }
        }

        var query = AccountVisibility.ProjectToVm(accounts, _context.Users.AsNoTracking())
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id);

        return await PaginatedList<AccountVm>.CreateAsync(
            query,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
