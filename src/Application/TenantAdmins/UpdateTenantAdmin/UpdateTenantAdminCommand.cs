using FluentValidation.Results;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;
using ValidationException = MyWealth.Application.Common.Exceptions.ValidationException;

namespace MyWealth.Application.TenantAdmins.UpdateTenantAdmin;

[Authorize(Roles = Roles.SystemAdmin)]
public record UpdateTenantAdminCommand : IRequest
{
    public int Id { get; init; }

    public string? Name { get; init; }

    public bool? IsEnabled { get; init; }
}

public class UpdateTenantAdminCommandHandler : IRequestHandler<UpdateTenantAdminCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public UpdateTenantAdminCommandHandler(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task Handle(UpdateTenantAdminCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.Role == UserRole.TenantAdmin, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }

        if (request.IsEnabled == false)
        {
            user.Disable();
        }
        else if (request.IsEnabled == true)
        {
            user.Enable();
        }

        if (request.Name is not null)
        {
            user.Rename(request.Name);
        }

        await _context.ExecuteInTransactionAsync(async ct =>
        {
            if (user.IdentityUserId is not null)
            {
                if (request.Name is not null)
                {
                    ThrowIfIdentityFailed(
                        await _identityService.UpdateDisplayNameAsync(user.IdentityUserId, user.Name, ct));
                }

                if (request.IsEnabled is bool isEnabled)
                {
                    ThrowIfIdentityFailed(
                        await _identityService.SetEnabledAsync(user.IdentityUserId, isEnabled, ct));
                }
            }

            await _context.SaveChangesAsync(ct);
        }, cancellationToken);
    }

    private static void ThrowIfIdentityFailed(Result result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new ValidationException(result.Errors.Select(e => new ValidationFailure("Request", e)));
    }
}
