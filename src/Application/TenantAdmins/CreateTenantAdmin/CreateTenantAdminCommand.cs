using FluentValidation.Results;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Models;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using ValidationException = MyWealth.Application.Common.Exceptions.ValidationException;

namespace MyWealth.Application.TenantAdmins.CreateTenantAdmin;

[Authorize(Roles = Roles.SystemAdmin)]
public record CreateTenantAdminCommand : IRequest<int>
{
    public int TenantId { get; init; }

    public string? Name { get; init; }

    public string? Email { get; init; }

    public string? Password { get; init; }
}

public class CreateTenantAdminCommandHandler : IRequestHandler<CreateTenantAdminCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public CreateTenantAdminCommandHandler(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<int> Handle(CreateTenantAdminCommand request, CancellationToken cancellationToken)
    {
        var createdId = 0;

        await _context.ExecuteInTransactionAsync(async ct =>
        {
            var user = User.CreateTenantAdmin(request.TenantId, request.Name!, request.Email!);
            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);

            var (result, identityUserId) = await _identityService.CreateLoginUserAsync(
                user.Email,
                request.Password!,
                user.Name,
                UserRole.TenantAdmin,
                request.TenantId,
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
