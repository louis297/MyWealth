using MyWealth.Application.Common.Interfaces;
using MyWealth.Domain.Entities;

namespace MyWealth.Application.Tenants.UpdateTenant;

public class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateTenantCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v)
            .Must(v => v.Name is not null || v.IsEnabled is not null)
            .WithMessage("At least one of Name or IsEnabled must be supplied.")
            .OverridePropertyName("Request");

        When(v => v.Name is not null, () =>
        {
            RuleFor(v => v.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .MaximumLength(Tenant.NameMaxLength)
                .MustAsync(BeUniqueName)
                .WithMessage("A tenant with this name already exists.");
        });
    }

    private async Task<bool> BeUniqueName(UpdateTenantCommand command, string? name, CancellationToken cancellationToken)
    {
        var trimmed = name!.Trim();

        return !await _context.Tenants
            .AnyAsync(t => t.Id != command.Id && t.Name == trimmed, cancellationToken);
    }
}
