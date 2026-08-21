using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;

namespace MyWealth.Application.Tenants.GetTenantById;

[Authorize(Roles = Roles.SystemAdmin)]
public record GetTenantByIdQuery : IRequest<TenantVm>
{
    public int Id { get; init; }
}

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTenantByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<TenantVm> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .Where(t => t.Id == request.Id)
            .ProjectTo<TenantVm>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            throw new NotFoundException(nameof(Tenant), request.Id);
        }

        return tenant;
    }
}
