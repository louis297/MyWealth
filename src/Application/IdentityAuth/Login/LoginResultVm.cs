namespace MyWealth.Application.IdentityAuth.Login;

public sealed class LoginResultVm
{
    public required string AccessToken { get; init; }

    public string TokenType { get; init; } = "Bearer";

    public int ExpiresIn { get; init; }

    public required string UserId { get; init; }

    public required string Email { get; init; }

    public required string DisplayName { get; init; }

    public required string Role { get; init; }

    public int? TenantId { get; init; }
}
