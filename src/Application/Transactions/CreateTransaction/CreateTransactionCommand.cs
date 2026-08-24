using FluentValidation.Results;
using MyWealth.Application.Accounts;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Application.Holdings;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using MyWealth.Domain.ValueObjects;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;
using ValidationException = MyWealth.Application.Common.Exceptions.ValidationException;

namespace MyWealth.Application.Transactions.CreateTransaction;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public record CreateTransactionCommand : IRequest<int>
{
    public int AccountId { get; init; }

    public int? HoldingId { get; init; }

    public DateOnly? BookedOn { get; init; }

    public TransactionType? Type { get; init; }

    public MoneyDto? Amount { get; init; }

    public decimal? Quantity { get; init; }

    public string? Note { get; init; }
}

public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateTransactionCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(CreateTransactionCommand request, CancellationToken cancellationToken)
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

        var amount = Money.Of(request.Amount!.Amount!.Value, request.Amount.Currency!);
        if (amount.Currency != account.Currency)
        {
            throw new ValidationException(
            [
                new ValidationFailure("Amount.Currency", "Amount currency must match the account currency.")
            ]);
        }

        try
        {
            var transaction = account.Post(
                request.Type!.Value,
                request.BookedOn!.Value,
                amount,
                request.HoldingId,
                request.Quantity,
                request.Note);

            await _context.SaveChangesAsync(cancellationToken);

            return transaction.Id;
        }
        catch (ArgumentException ex)
        {
            throw new ValidationException(
            [
                new ValidationFailure(ex.ParamName ?? "Request", ex.Message)
            ]);
        }
    }
}
