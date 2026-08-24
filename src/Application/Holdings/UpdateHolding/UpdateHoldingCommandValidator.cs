using MyWealth.Domain.ValueObjects;

namespace MyWealth.Application.Holdings.UpdateHolding;

public class UpdateHoldingCommandValidator : AbstractValidator<UpdateHoldingCommand>
{
    public UpdateHoldingCommandValidator()
    {
        RuleFor(v => v)
            .Must(v => v.Instrument is not null || v.Quantity is not null || v.CostBasis?.Amount is not null)
            .WithMessage("At least one of instrument, quantity or costBasis.amount must be supplied.")
            .OverridePropertyName("Request");

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

        When(v => v.Quantity is not null, () =>
        {
            RuleFor(v => v.Quantity)
                .GreaterThanOrEqualTo(0);
        });

        When(v => v.CostBasis is not null, () =>
        {
            RuleFor(v => v.CostBasis!.Currency)
                .Null()
                .WithMessage("Cost basis currency cannot be changed.");

            When(v => v.CostBasis!.Amount is not null, () =>
            {
                RuleFor(v => v.CostBasis!.Amount)
                    .GreaterThanOrEqualTo(0);
            });
        });
    }
}
