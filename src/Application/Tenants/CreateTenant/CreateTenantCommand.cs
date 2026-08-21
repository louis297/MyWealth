using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;

namespace MyWealth.Application.Tenants.CreateTenant;

[Authorize(Roles = Roles.SystemAdmin)]
public record CreateTenantCommand : IRequest<int>
{
    public string? Name { get; init; }
}

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateTenantCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = Tenant.Create(request.Name!);

        _context.Tenants.Add(tenant);

        await _context.SaveChangesAsync(cancellationToken);

        return tenant.Id;
    }
}
