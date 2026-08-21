using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Mappings;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;

namespace MyWealth.Application.Tenants.GetTenants;

[Authorize(Roles = Roles.SystemAdmin)]
public record GetTenantsQuery : IRequest<PaginatedList<TenantVm>>
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public bool? IsEnabled { get; init; }

    public string? Search { get; init; }
}

public class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, PaginatedList<TenantVm>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTenantsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<TenantVm>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Tenants.AsQueryable();

        if (request.IsEnabled is bool isEnabled)
        {
            query = query.Where(t => t.IsEnabled == isEnabled);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            if (int.TryParse(search, out var id))
            {
                query = query.Where(t => t.Id == id || t.Name.Contains(search));
            }
            else
            {
                query = query.Where(t => t.Name.Contains(search));
            }
        }

        query = query.OrderBy(t => t.Name).ThenBy(t => t.Id);

        return await query.ProjectToListAsync<TenantVm>(
            _mapper.ConfigurationProvider,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
