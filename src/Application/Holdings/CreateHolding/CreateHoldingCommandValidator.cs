using MyWealth.Domain.ValueObjects;

namespace MyWealth.Application.Holdings.CreateHolding;

public class CreateHoldingCommandValidator : AbstractValidator<CreateHoldingCommand>
{
    public CreateHoldingCommandValidator()
    {
        RuleFor(v => v.AccountId)
            .GreaterThan(0);

        RuleFor(v => v.Instrument)
            .NotNull();

        When(v => v.Instrument is not null, () =>
        {
            RuleFor(v => v.Instrument!.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MaximumLength(Instrument.NameMaxLength);

            When(v => v.Instrument!.Symbol is not null, () =>
            {
                RuleFor(v => v.Instrument!.Symbol)
                    .MaximumLength(Instrument.SymbolMaxLength);
            });
        });

        RuleFor(v => v.Quantity)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .GreaterThanOrEqualTo(0);

        RuleFor(v => v.CostBasis)
            .NotNull();

        When(v => v.CostBasis is not null, () =>
        {
            RuleFor(v => v.CostBasis!.Amount)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .GreaterThanOrEqualTo(0);

            RuleFor(v => v.CostBasis!.Currency)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(BeIso4217)
                .WithMessage("Currency must be a 3-letter ISO 4217 code.");
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
