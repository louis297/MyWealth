using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;

namespace MyWealth.Application.TenantAdmins.GetTenantAdminById;

[Authorize(Roles = Roles.SystemAdmin)]
public record GetTenantAdminByIdQuery : IRequest<TenantAdminVm>
{
    public int Id { get; init; }
}

public class GetTenantAdminByIdQueryHandler : IRequestHandler<GetTenantAdminByIdQuery, TenantAdminVm>
{
    private readonly IApplicationDbContext _context;

    public GetTenantAdminByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantAdminVm> Handle(GetTenantAdminByIdQuery request, CancellationToken cancellationToken)
    {
        var admin = await TenantAdminProjection
            .ProjectToVm(
                TenantAdminProjection.TenantAdmins(_context.Users.AsNoTracking()).Where(u => u.Id == request.Id),
                _context.Tenants.AsNoTracking())
            .FirstOrDefaultAsync(cancellationToken);

        if (admin is null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }

        return admin;
    }
}
