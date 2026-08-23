using MyWealth.Application.Common.Interfaces;
using MyWealth.Domain.Entities;

namespace MyWealth.Application.TenantAdmins.CreateTenantAdmin;

public class CreateTenantAdminCommandValidator : AbstractValidator<CreateTenantAdminCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateTenantAdminCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.TenantId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
            .MustAsync(TenantExists)
            .WithMessage("Tenant was not found.")
            .MustAsync(TenantIsEnabled)
            .WithMessage("Cannot create a tenant admin for a disabled tenant.");

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

    private async Task<bool> TenantExists(int tenantId, CancellationToken cancellationToken)
        => await _context.Tenants.AnyAsync(t => t.Id == tenantId, cancellationToken);

    private async Task<bool> TenantIsEnabled(int tenantId, CancellationToken cancellationToken)
        => await _context.Tenants.AnyAsync(t => t.Id == tenantId && t.IsEnabled, cancellationToken);

    private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
    {
        var trimmed = email.Trim();

        return !await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == trimmed, cancellationToken);
    }
}
