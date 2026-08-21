namespace MyWealth.Application.Common.Models;

public sealed class AuthenticationResult
{
    public bool Succeeded { get; private init; }

    public bool IsCustomer { get; private init; }

    public bool IsDisabled { get; private init; }

    public string? AccessToken { get; private init; }

    public int ExpiresIn { get; private init; }

    public string? UserId { get; private init; }

    public string? Email { get; private init; }

    public string? DisplayName { get; private init; }

    public string? Role { get; private init; }

    public int? TenantId { get; private init; }

    public static AuthenticationResult Failed() => new();

    public static AuthenticationResult Customer() => new() { IsCustomer = true };

    public static AuthenticationResult Disabled() => new() { IsDisabled = true };

    public static AuthenticationResult Success(
        string accessToken,
        int expiresIn,
        string userId,
        string email,
        string displayName,
        string role,
        int? tenantId) =>
        new()
        {
            Succeeded = true,
            AccessToken = accessToken,
            ExpiresIn = expiresIn,
            UserId = userId,
            Email = email,
            DisplayName = displayName,
            Role = role,
            TenantId = tenantId
        };
}
