using MyWealth.Domain.Events;

namespace MyWealth.Domain.Entities;

public class Tenant : BaseAuditableEntity
{
    public const int NameMaxLength = 200;

    public string Name { get; private set; } = null!;

    public bool IsEnabled { get; private set; }

    private Tenant()
    {
    }

    public static Tenant Create(string name)
    {
        var tenant = new Tenant { IsEnabled = true };
        tenant.SetName(name);
        tenant.AddDomainEvent(new TenantCreatedEvent(tenant));
        return tenant;
    }

    public void Rename(string name) => SetName(name);

    public void Enable()
    {
        if (IsEnabled)
        {
            return;
        }

        IsEnabled = true;
        AddDomainEvent(new TenantEnabledEvent(this));
    }

    public void Disable()
    {
        if (!IsEnabled)
        {
            return;
        }

        IsEnabled = false;
        AddDomainEvent(new TenantDisabledEvent(this));
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new ArgumentException($"Name must be {NameMaxLength} characters or fewer.", nameof(name));
        }

        Name = trimmed;
    }
}
