namespace MyWealth.Application.IdentityAuth.UpdateCurrentUser;

public class UpdateCurrentUserCommandValidator : AbstractValidator<UpdateCurrentUserCommand>
{
    public UpdateCurrentUserCommandValidator()
    {
        RuleFor(v => v.DisplayName)
            .NotEmpty()
            .MaximumLength(200);
    }
}
