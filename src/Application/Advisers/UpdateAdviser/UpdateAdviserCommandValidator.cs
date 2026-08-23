using MyWealth.Domain.Entities;

namespace MyWealth.Application.Advisers.UpdateAdviser;

public class UpdateAdviserCommandValidator : AbstractValidator<UpdateAdviserCommand>
{
    public UpdateAdviserCommandValidator()
    {
        RuleFor(v => v)
            .Must(v => v.Name is not null || v.IsEnabled is not null)
            .WithMessage("At least one of Name or IsEnabled must be supplied.")
            .OverridePropertyName("Request");

        When(v => v.Name is not null, () =>
        {
            RuleFor(v => v.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MaximumLength(User.NameMaxLength);
        });
    }
}
