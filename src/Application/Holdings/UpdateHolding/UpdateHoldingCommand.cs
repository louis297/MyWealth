using FluentValidation.Results;
using MyWealth.Application.Accounts;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using MyWealth.Domain.ValueObjects;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;
using ValidationException = MyWealth.Application.Common.Exceptions.ValidationException;

namespace MyWealth.Application.Holdings.UpdateHolding;

[Authorize(Roles = AccountVisibility.AllowedRoles)]
public record UpdateHoldingCommand : IRequest
{
    public int AccountId { get; init; }

    public int Id { get; init; }

    public InstrumentDto? Instrument { get; init; }

    public decimal? Quantity { get; init; }

    public MoneyDto? CostBasis { get; init; }
}

public class UpdateHoldingCommandHandler : IRequestHandler<UpdateHoldingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateHoldingCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateHoldingCommand request, CancellationToken cancellationToken)
    {
        var account = await AccountVisibility.FindVisibleAccountWithHoldingsAsync(
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

        var instrument = request.Instrument is null
            ? null
            : Instrument.Create(request.Instrument.Name!, request.Instrument.Symbol);

        var updated = account.UpdateHolding(
            request.Id,
            instrument,
            request.Quantity,
            request.CostBasis?.Amount);

        if (!updated)
        {
            throw new NotFoundException(nameof(Holding), request.Id);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
