namespace MyWealth.Application.Common.Models;

public sealed class CurrentUserDto
{
    public required string UserId { get; init; }

    public required string Email { get; init; }

    public required string DisplayName { get; init; }

    public required string Role { get; init; }

    public int? TenantId { get; init; }

    public bool IsEnabled { get; init; }
}
