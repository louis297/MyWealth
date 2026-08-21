using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;

namespace MyWealth.Application.Tenants.UpdateTenant;

[Authorize(Roles = Roles.SystemAdmin)]
public record UpdateTenantCommand : IRequest
{
    public int Id { get; init; }

    public string? Name { get; init; }

    public bool? IsEnabled { get; init; }
}

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateTenantCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (tenant is null)
        {
            throw new NotFoundException(nameof(Tenant), request.Id);
        }

        if (request.Name is not null)
        {
            tenant.Rename(request.Name);
        }

        if (request.IsEnabled is bool isEnabled)
        {
            if (isEnabled)
            {
                tenant.Enable();
            }
            else
            {
                tenant.Disable();
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
