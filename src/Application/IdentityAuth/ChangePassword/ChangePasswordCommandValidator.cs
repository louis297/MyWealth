namespace MyWealth.Application.IdentityAuth.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(v => v.CurrentPassword)
            .NotEmpty();

        RuleFor(v => v.NewPassword)
            .NotEmpty()
            .NotEqual(v => v.CurrentPassword);
    }
}
