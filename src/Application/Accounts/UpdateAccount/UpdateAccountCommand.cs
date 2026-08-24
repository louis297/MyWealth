using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;

namespace MyWealth.Application.Accounts.UpdateAccount;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public record UpdateAccountCommand : IRequest
{
    public int Id { get; init; }

    public string? Name { get; init; }

    public AccountType? Type { get; init; }

    public string? Currency { get; init; }

    public int? CustomerId { get; init; }

    public AccountStatus? Status { get; init; }
}

public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateAccountCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await AccountVisibility.FindVisibleAccountAsync(
            _context, _user, request.Id, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(nameof(Account), request.Id);
        }

        if (request.Name is not null)
        {
            account.Rename(request.Name);
        }

        if (request.Type is AccountType type)
        {
            account.ChangeType(type);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
