using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Mappings;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.Advisers.GetAdvisers;

[Authorize(Roles = Roles.TenantAdmin)]
public class GetAdvisersQuery : IRequest<PaginatedList<AdviserVm>>
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public bool? IsEnabled { get; init; }

    public string? Search { get; init; }
}

public class GetAdvisersQueryHandler : IRequestHandler<GetAdvisersQuery, PaginatedList<AdviserVm>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAdvisersQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PaginatedList<AdviserVm>> Handle(GetAdvisersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Adviser);

        if (request.IsEnabled is bool isEnabled)
        {
            query = query.Where(u => u.IsEnabled == isEnabled);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            if (int.TryParse(search, out var id))
            {
                query = query.Where(u => u.Id == id || u.Name.Contains(search) || u.Email.Contains(search));
            }
            else
            {
                query = query.Where(u => u.Name.Contains(search) || u.Email.Contains(search));
            }
        }

        query = query.OrderBy(u => u.Name).ThenBy(u => u.Id);

        return await query.ProjectToListAsync<AdviserVm>(
            _mapper.ConfigurationProvider,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
