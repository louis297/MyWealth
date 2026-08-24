using MyWealth.Application.Common.Interfaces;
using MyWealth.Domain.Entities;

namespace MyWealth.Application.Accounts.CreateAccount;

public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateAccountCommandValidator(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;

        RuleFor(v => v.CustomerId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
            .MustAsync(BeEnabledCustomerInTenant)
            .WithMessage("Customer must be an enabled customer in the current tenant.")
            .MustAsync(CallerMayTarget)
            .WithMessage("Advisers may only create accounts for their own customers.");

        RuleFor(v => v.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(Account.NameMaxLength);

        RuleFor(v => v.Type)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .IsInEnum();

        RuleFor(v => v.Currency)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(BeIso4217)
            .WithMessage("Currency must be a 3-letter ISO 4217 code.");
    }

    private Task<bool> BeEnabledCustomerInTenant(int customerId, CancellationToken cancellationToken)
        => AccountVisibility.IsEnabledCustomerInTenantAsync(_context, customerId, cancellationToken);

    private Task<bool> CallerMayTarget(int customerId, CancellationToken cancellationToken)
        => AccountVisibility.CallerMayTargetCustomerAsync(_context, _user, customerId, cancellationToken);

    private static bool BeIso4217(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return false;
        }

        var normalised = currency.Trim();
        return normalised.Length == Account.CurrencyLength && normalised.All(char.IsAsciiLetter);
    }
}
