using MyWealth.Domain.Entities;

namespace MyWealth.Application.Accounts.UpdateAccount;

public class UpdateAccountCommandValidator : AbstractValidator<UpdateAccountCommand>
{
    public UpdateAccountCommandValidator()
    {
        RuleFor(v => v)
            .Must(v => v.Name is not null || v.Type is not null)
            .WithMessage("At least one of Name or Type must be supplied.")
            .OverridePropertyName("Request");

        RuleFor(v => v.Currency)
            .Null()
            .WithMessage("Currency cannot be changed.");

        RuleFor(v => v.CustomerId)
            .Null()
            .WithMessage("Customer cannot be changed.");

        RuleFor(v => v.Status)
            .Null()
            .WithMessage("Status cannot be changed via update. Use the close action.");

        When(v => v.Name is not null, () =>
        {
            RuleFor(v => v.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MaximumLength(Account.NameMaxLength);
        });

        When(v => v.Type is not null, () =>
        {
            RuleFor(v => v.Type)
                .IsInEnum();
        });
    }
}
