using FluentValidation.Results;
using MyWealth.Application.Accounts;
using MyWealth.Application.Common.Exceptions;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using MyWealth.Domain.ValueObjects;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;
using ValidationException = MyWealth.Application.Common.Exceptions.ValidationException;

namespace MyWealth.Application.Holdings.CreateHolding;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public record CreateHoldingCommand : IRequest<int>
{
    public int AccountId { get; init; }

    public InstrumentDto? Instrument { get; init; }

    public decimal? Quantity { get; init; }

    public MoneyDto? CostBasis { get; init; }
}

public class CreateHoldingCommandHandler : IRequestHandler<CreateHoldingCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateHoldingCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(CreateHoldingCommand request, CancellationToken cancellationToken)
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

        var costBasis = Money.Of(request.CostBasis!.Amount!.Value, request.CostBasis.Currency!);
        if (costBasis.Currency != account.Currency)
        {
            throw new ValidationException(
            [
                new ValidationFailure("CostBasis.Currency", "Cost basis currency must match the account currency.")
            ]);
        }

        var holding = account.AddHolding(
            Instrument.Create(request.Instrument!.Name!, request.Instrument.Symbol),
            request.Quantity!.Value,
            costBasis);

        await _context.SaveChangesAsync(cancellationToken);

        return holding.Id;
    }
}
