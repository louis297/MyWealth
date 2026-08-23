using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Common.Security;

namespace MyWealth.Application.Customers.GetCustomers;

[Authorize(Roles = CustomerVisibility.AllowedRoles)]
public class GetCustomersQuery : IRequest<PaginatedList<CustomerVm>>
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public bool? IsEnabled { get; init; }

    public string? Search { get; init; }
}

public class GetCustomersQueryHandler : IRequestHandler<GetCustomersQuery, PaginatedList<CustomerVm>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetCustomersQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<PaginatedList<CustomerVm>> Handle(GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var adviserDomainUserId = CustomerVisibility.IsAdviser(_user)
            ? await CustomerVisibility.GetCallerDomainUserIdAsync(_context, _user, cancellationToken)
            : null;

        var customers = CustomerVisibility.ScopedCustomers(
            _context.Users.AsNoTracking(), _user, adviserDomainUserId);

        if (request.IsEnabled is bool isEnabled)
        {
            customers = customers.Where(u => u.IsEnabled == isEnabled);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            if (int.TryParse(search, out var id))
            {
                customers = customers.Where(u => u.Id == id || u.Name.Contains(search) || u.Email.Contains(search));
            }
            else
            {
                customers = customers.Where(u => u.Name.Contains(search) || u.Email.Contains(search));
            }
        }

        var query = CustomerVisibility.ProjectToVm(customers, _context.Users.AsNoTracking())
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id);

        return await PaginatedList<CustomerVm>.CreateAsync(
            query,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
