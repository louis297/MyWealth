using MyWealth.Domain.Enums;

namespace MyWealth.Domain.Constants;

/// <summary>
/// String names of <see cref="UserRole"/>, used in JWT role claims and
/// <c>[Authorize(Roles = …)]</c>. The source of truth for a user's role is the
/// <c>UserRole</c> column on the user, not ASP.NET Identity roles.
/// </summary>
public abstract class Roles
{
    public const string SystemAdmin = nameof(UserRole.SystemAdmin);
    public const string TenantAdmin = nameof(UserRole.TenantAdmin);
    public const string Adviser = nameof(UserRole.Adviser);
    public const string Customer = nameof(UserRole.Customer);
}
