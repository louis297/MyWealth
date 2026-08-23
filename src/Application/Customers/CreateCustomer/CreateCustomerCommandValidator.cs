using MyWealth.Application.Common.Interfaces;
using MyWealth.Domain.Entities;

namespace MyWealth.Application.Customers.CreateCustomer;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateCustomerCommandValidator(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;

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

        RuleFor(v => v.AdviserId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
            .MustAsync(BeEnabledAdviserInTenant)
            .WithMessage("Adviser must be an enabled adviser in the current tenant.")
            .MustAsync(CallerMayAssign)
            .WithMessage("Advisers may only assign customers to themselves.");
    }

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        var trimmed = email.Trim();

        return !await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == trimmed, cancellationToken);
    }

    private Task<bool> BeEnabledAdviserInTenant(int adviserId, CancellationToken cancellationToken)
        => CustomerVisibility.IsEnabledAdviserAsync(_context, adviserId, cancellationToken);

    private Task<bool> CallerMayAssign(int adviserId, CancellationToken cancellationToken)
        => CustomerVisibility.CallerMayAssignAsync(_context, _user, adviserId, cancellationToken);
}
