using MyWealth.Application.Common.Interfaces;
using MyWealth.Domain.Entities;

namespace MyWealth.Application.Advisers.CreateAdviser;

public class CreateAdviserCommandValidator : AbstractValidator<CreateAdviserCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateAdviserCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(User.NameMaxLength);

        RuleFor(v => v.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(User.EmailMaxLength)
            .MustAsync(BeUniqueEmail)
            .WithMessage("A user with this email already exists.");

        RuleFor(v => v.Password)
            .NotEmpty();
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        var trimmed = email.Trim();

        return !await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == trimmed, cancellationToken);
    }
}
