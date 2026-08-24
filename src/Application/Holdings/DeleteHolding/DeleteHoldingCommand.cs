using FluentValidation.Results;
using MyWealth.Application.Accounts;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;
using ValidationException = MyWealth.Application.Common.Exceptions.ValidationException;

namespace MyWealth.Application.Holdings.DeleteHolding;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public record DeleteHoldingCommand : IRequest
{
    public int AccountId { get; init; }

    public int Id { get; init; }
}

public class DeleteHoldingCommandHandler : IRequestHandler<DeleteHoldingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DeleteHoldingCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DeleteHoldingCommand request, CancellationToken cancellationToken)
    {
        var account = await AccountVisibility.FindVisibleAccountAggregateAsync(
            _context, _user, request.AccountId, cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(nameof(Account), request.AccountId);
        }

        if (account.Status == AccountStatus.Closed)
        {
            throw new ValidationException(
            [
                new ValidationFailure("AccountId", "Closed accounts reject writes.")
            ]);
        }

        if (account.Transactions.Any(t => t.HoldingId == request.Id))
        {
            throw new ValidationException(
            [
                new ValidationFailure("Id", "Cannot delete a holding that still has historical transactions.")
            ]);
        }

        if (!account.RemoveHolding(request.Id))
        {
            throw new NotFoundException(nameof(Holding), request.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
