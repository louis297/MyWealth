using MyWealth.Application.Common.Interfaces;
using MyWealth.Domain.Entities;

namespace MyWealth.Application.Customers.UpdateCustomer;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateCustomerCommandValidator(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;

        RuleFor(v => v)
            .Must(v => v.Name is not null || v.IsEnabled is not null || v.AdviserId is not null)
            .WithMessage("At least one of Name, IsEnabled or AdviserId must be supplied.")
            .OverridePropertyName("Request");

        When(v => v.Name is not null, () =>
        {
            RuleFor(v => v.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MaximumLength(User.NameMaxLength);
        });

        When(v => v.AdviserId is not null, () =>
        {
            RuleFor(v => v.AdviserId!.Value)
                .Cascade(CascadeMode.Stop)
                .GreaterThan(0)
                .MustAsync(BeEnabledAdviserInTenant)
                .WithMessage("Adviser must be an enabled adviser in the current tenant.")
                .MustAsync(CallerMayAssign)
                .WithMessage("Advisers may only assign customers to themselves.")
                .OverridePropertyName(nameof(UpdateCustomerCommand.AdviserId));
        });
    }

    private Task<bool> BeEnabledAdviserInTenant(int adviserId, CancellationToken cancellationToken)
        => CustomerVisibility.IsEnabledAdviserAsync(_context, adviserId, cancellationToken);

    private Task<bool> CallerMayAssign(int adviserId, CancellationToken cancellationToken)
        => CustomerVisibility.CallerMayAssignAsync(_context, _user, adviserId, cancellationToken);
}
