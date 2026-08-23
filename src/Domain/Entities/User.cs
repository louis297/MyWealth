using MyWealth.Domain.Events;

namespace MyWealth.Domain.Entities;

public class User : BaseAuditableEntity
{
    public const int NameMaxLength = 200;
    public const int EmailMaxLength = 256;

    public int? TenantId { get; private set; }

    public string Name { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public bool IsEnabled { get; private set; }

    public UserRole Role { get; private set; }

    public int? AdviserId { get; private set; }

    public string? IdentityUserId { get; private set; }

    private User()
    {
    }

    public static User CreateSystemAdmin(string name, string email)
        => Create(name, email, UserRole.SystemAdmin, tenantId: null, adviserId: null);

    public static User CreateTenantAdmin(int tenantId, string name, string email)
        => Create(name, email, UserRole.TenantAdmin, tenantId, adviserId: null);

    public static User CreateAdviser(int tenantId, string name, string email)
        => Create(name, email, UserRole.Adviser, tenantId, adviserId: null);

    public static User CreateCustomer(int tenantId, int adviserId, string name, string email)
        => Create(name, email, UserRole.Customer, tenantId, adviserId);

    public void Rename(string name) => SetName(name);

    public void ReassignAdviser(int adviserId)
    {
        if (Role != UserRole.Customer)
        {
            throw new InvalidOperationException("Only customers can be reassigned.");
        }

        if (adviserId <= 0)
        {
            throw new ArgumentException("Adviser is required.", nameof(adviserId));
        }

        if (AdviserId == adviserId)
        {
            return;
        }

        AdviserId = adviserId;
        AddDomainEvent(new CustomerReassignedEvent(this));
    }

    public void Enable()
    {
        if (IsEnabled)
        {
            return;
        }

        IsEnabled = true;
        AddDomainEvent(new UserEnabledEvent(this));
    }

    public void Disable(int assignedCustomerCount = 0)
    {
        if (Role == UserRole.Adviser && assignedCustomerCount > 0)
        {
            throw new InvalidOperationException(
                "Cannot disable an adviser who still has customers assigned.");
        }

        if (!IsEnabled)
        {
            return;
        }

        IsEnabled = false;
        AddDomainEvent(new UserDisabledEvent(this));
    }

    public void LinkIdentity(string identityUserId)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            throw new ArgumentException("Identity user id is required.", nameof(identityUserId));
        }

        IdentityUserId = identityUserId.Trim();
    }

    private static User Create(string name, string email, UserRole role, int? tenantId, int? adviserId)
    {
        ValidateRoleBindings(role, tenantId, adviserId);

        var user = new User
        {
            Role = role,
            TenantId = tenantId,
            AdviserId = adviserId,
            IsEnabled = true
        };

        user.SetName(name);
        user.SetEmail(email);
        user.AddDomainEvent(new UserCreatedEvent(user));
        return user;
    }

    private static void ValidateRoleBindings(UserRole role, int? tenantId, int? adviserId)
    {
        if (role == UserRole.SystemAdmin)
        {
            if (tenantId is not null)
            {
                throw new ArgumentException("SystemAdmin must not have a tenant.", nameof(tenantId));
            }

            if (adviserId is not null)
            {
                throw new ArgumentException("SystemAdmin must not have an adviser.", nameof(adviserId));
            }

            return;
        }

        if (tenantId is null or <= 0)
        {
            throw new ArgumentException("Tenant is required.", nameof(tenantId));
        }

        if (role == UserRole.Customer)
        {
            if (adviserId is null or <= 0)
            {
                throw new ArgumentException("Adviser is required.", nameof(adviserId));
            }

            return;
        }

        if (adviserId is not null)
        {
            throw new ArgumentException($"{role} must not have an adviser.", nameof(adviserId));
        }
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

    private void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var trimmed = email.Trim();
        if (trimmed.Length > EmailMaxLength)
        {
            throw new ArgumentException($"Email must be {EmailMaxLength} characters or fewer.", nameof(email));
        }

        Email = trimmed;
    }
}
