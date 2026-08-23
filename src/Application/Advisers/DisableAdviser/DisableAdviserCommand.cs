using FluentValidation.Results;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;
using ValidationException = MyWealth.Application.Common.Exceptions.ValidationException;

namespace MyWealth.Application.Advisers.DisableAdviser;

[Authorize(Roles = Roles.TenantAdmin)]
public record DisableAdviserCommand : IRequest
{
    public int Id { get; init; }
}

public class DisableAdviserCommandHandler : IRequestHandler<DisableAdviserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public DisableAdviserCommandHandler(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task Handle(DisableAdviserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.Role == UserRole.Adviser, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }

        var assignedCustomers = await _context.Users
            .CountAsync(u => u.AdviserId == user.Id, cancellationToken);

        if (assignedCustomers > 0)
        {
            throw new ValidationException(
            [
                new ValidationFailure("Id", "Cannot disable an adviser who still has customers assigned.")
            ]);
        }

        await _context.ExecuteInTransactionAsync(async ct =>
        {
            user.Disable(assignedCustomers);

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
