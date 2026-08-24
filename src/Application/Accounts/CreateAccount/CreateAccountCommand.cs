using MyWealth.Application.Common.Exceptions;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.Accounts.CreateAccount;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public record CreateAccountCommand : IRequest<int>
{
    public int CustomerId { get; init; }

    public string? Name { get; init; }

    public AccountType? Type { get; init; }

    public string? Currency { get; init; }
}

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateAccountCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        if (_user.TenantId is not int)
        {
            throw new ForbiddenAccessException();
        }

        var customer = await _context.Users
            .FirstAsync(u => u.Id == request.CustomerId && u.Role == UserRole.Customer, cancellationToken);

        var account = Account.Open(
            customer.TenantId!.Value,
            customer.Id,
            request.Name!,
            request.Type!.Value,
            request.Currency!);

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
