using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using MyWealth.Domain.ValueObjects;

namespace MyWealth.Application.Transactions.CreateTransaction;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(v => v.AccountId)
            .GreaterThan(0);

        RuleFor(v => v.BookedOn)
            .NotNull();

        RuleFor(v => v.Type)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .IsInEnum();

        RuleFor(v => v.Amount)
            .NotNull();

        When(v => v.Amount is not null, () =>
        {
            RuleFor(v => v.Amount!.Amount)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .GreaterThan(0);

            RuleFor(v => v.Amount!.Currency)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(BeIso4217)
                .WithMessage("Currency must be a 3-letter ISO 4217 code.");
        });

        When(v => v.Note is not null, () =>
        {
            RuleFor(v => v.Note)
                .MaximumLength(Transaction.NoteMaxLength);
        });

        When(v => v.Type is TransactionType.Buy or TransactionType.Sell, () =>
        {
            RuleFor(v => v.HoldingId)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .GreaterThan(0);

            RuleFor(v => v.Quantity)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .GreaterThan(0);
        });

        When(v => v.Type is TransactionType.TransferIn
            or TransactionType.TransferOut
            or TransactionType.Dividend
            or TransactionType.Interest, () =>
        {
            RuleFor(v => v.HoldingId)
                .Null();

            RuleFor(v => v.Quantity)
                .Null();
        });
    }

    private static bool BeIso4217(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return false;
        }

        var normalised = currency.Trim();
        return normalised.Length == Money.CurrencyLength && normalised.All(char.IsAsciiLetter);
    }
}
