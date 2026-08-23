using FluentValidation.Results;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;
using ValidationException = MyWealth.Application.Common.Exceptions.ValidationException;

namespace MyWealth.Application.TenantAdmins.DisableTenantAdmin;

[Authorize(Roles = Roles.SystemAdmin)]
public record DisableTenantAdminCommand : IRequest
{
    public int Id { get; init; }
}

public class DisableTenantAdminCommandHandler : IRequestHandler<DisableTenantAdminCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public DisableTenantAdminCommandHandler(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task Handle(DisableTenantAdminCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.Role == UserRole.TenantAdmin, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }

        await _context.ExecuteInTransactionAsync(async ct =>
        {
            user.Disable();

            if (user.IdentityUserId is not null)
            {
                var result = await _identityService.SetEnabledAsync(user.IdentityUserId, false, ct);
                if (!result.Succeeded)
                {
                    throw new ValidationException(result.Errors.Select(e => new ValidationFailure("Request", e)));
                }
            }

            await _context.SaveChangesAsync(ct);
        }, cancellationToken);
    }
}
