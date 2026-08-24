using FluentValidation.Results;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;
using ValidationException = MyWealth.Application.Common.Exceptions.ValidationException;

namespace MyWealth.Application.Accounts.CloseAccount;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public record CloseAccountCommand : IRequest
{
    public int Id { get; init; }
}

public class CloseAccountCommandHandler : IRequestHandler<CloseAccountCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CloseAccountCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(CloseAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await AccountVisibility.FindVisibleAccountAsync(
            _context, _user, request.Id, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(nameof(Account), request.Id);
        }

        if (account.Status == AccountStatus.Closed)
        {
            throw new ValidationException(
            [
                new ValidationFailure("Id", "Account is already closed.")
            ]);
        }

        account.Close();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
