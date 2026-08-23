using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;

namespace MyWealth.Application.TenantAdmins.GetTenantAdmins;

[Authorize(Roles = Roles.SystemAdmin)]
public class GetTenantAdminsQuery : IRequest<PaginatedList<TenantAdminVm>>
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public bool? IsEnabled { get; init; }

    public int? TenantId { get; init; }

    public string? Search { get; init; }
}

public class GetTenantAdminsQueryHandler : IRequestHandler<GetTenantAdminsQuery, PaginatedList<TenantAdminVm>>
{
    private readonly IApplicationDbContext _context;

    public GetTenantAdminsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<TenantAdminVm>> Handle(
        GetTenantAdminsQuery request,
        CancellationToken cancellationToken)
    {
        var admins = TenantAdminProjection.TenantAdmins(_context.Users.AsNoTracking());

        if (request.IsEnabled is bool isEnabled)
        {
            admins = admins.Where(u => u.IsEnabled == isEnabled);
        }

        if (request.TenantId is int tenantId)
        {
            admins = admins.Where(u => u.TenantId == tenantId);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            if (int.TryParse(search, out var id))
            {
                admins = admins.Where(u => u.Id == id || u.Name.Contains(search) || u.Email.Contains(search));
            }
            else
            {
                admins = admins.Where(u => u.Name.Contains(search) || u.Email.Contains(search));
            }
        }

        var query = TenantAdminProjection
            .ProjectToVm(admins, _context.Tenants.AsNoTracking())
            .OrderBy(a => a.Name)
            .ThenBy(a => a.Id);

        return await PaginatedList<TenantAdminVm>.CreateAsync(
            query,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
