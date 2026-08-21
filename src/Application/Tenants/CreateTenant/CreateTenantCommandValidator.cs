using MyWealth.Application.Common.Interfaces;
using MyWealth.Domain.Entities;

namespace MyWealth.Application.Tenants.CreateTenant;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateTenantCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(Tenant.NameMaxLength)
            .MustAsync(BeUniqueName)
            .WithMessage("A tenant with this name already exists.");
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();

        return !await _context.Tenants.AnyAsync(t => t.Name == trimmed, cancellationToken);
    }
}
