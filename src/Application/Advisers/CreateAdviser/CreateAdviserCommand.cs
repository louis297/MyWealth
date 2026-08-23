using FluentValidation.Results;
using MyWealth.Application.Common.Exceptions;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using ValidationException = MyWealth.Application.Common.Exceptions.ValidationException;

namespace MyWealth.Application.Advisers.CreateAdviser;

[Authorize(Roles = Roles.TenantAdmin)]
public record CreateAdviserCommand : IRequest<int>
{
    public string? Name { get; init; }

    public string? Email { get; init; }

    public string? Password { get; init; }
}

public class CreateAdviserCommandHandler : IRequestHandler<CreateAdviserCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly IUser _user;

    public CreateAdviserCommandHandler(
        IApplicationDbContext context,
        IIdentityService identityService,
        IUser user)
    {
        _context = context;
        _identityService = identityService;
        _user = user;
    }

    public async Task<int> Handle(CreateAdviserCommand request, CancellationToken cancellationToken)
    {
        if (_user.TenantId is not int tenantId)
        {
            throw new ForbiddenAccessException();
        }

        var createdId = 0;

        await _context.ExecuteInTransactionAsync(async ct =>
        {
            var user = User.CreateAdviser(tenantId, request.Name!, request.Email!);
            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);

            var (result, identityUserId) = await _identityService.CreateLoginUserAsync(
                user.Email,
                request.Password!,
                user.Name,
                UserRole.Adviser,
                tenantId,
                ct);

            ThrowIfIdentityFailed(result);

            user.LinkIdentity(identityUserId);
            await _context.SaveChangesAsync(ct);
            createdId = user.Id;
        }, cancellationToken);

        return createdId;
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
