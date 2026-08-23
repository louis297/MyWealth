using FluentValidation.Results;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;
using ValidationException = MyWealth.Application.Common.Exceptions.ValidationException;

namespace MyWealth.Application.Advisers.UpdateAdviser;

[Authorize(Roles = Roles.TenantAdmin)]
public record UpdateAdviserCommand : IRequest
{
    public int Id { get; init; }

    public string? Name { get; init; }

    public bool? IsEnabled { get; init; }
}

public class UpdateAdviserCommandHandler : IRequestHandler<UpdateAdviserCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public UpdateAdviserCommandHandler(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task Handle(UpdateAdviserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id && u.Role == UserRole.Adviser, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }

        if (request.IsEnabled == false)
        {
            var assignedCustomers = await CountAssignedCustomers(user.Id, cancellationToken);
            if (assignedCustomers > 0)
            {
                throw new ValidationException(
                [
                    new ValidationFailure("Id", "Cannot disable an adviser who still has customers assigned.")
                ]);
            }

            user.Disable(assignedCustomers);
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

    private Task<int> CountAssignedCustomers(int adviserId, CancellationToken cancellationToken)
        => _context.Users.CountAsync(u => u.AdviserId == adviserId, cancellationToken);

    private static void ThrowIfIdentityFailed(Result result)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new ValidationException(result.Errors.Select(e => new ValidationFailure("Request", e)));
    }
}
