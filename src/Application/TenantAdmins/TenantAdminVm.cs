namespace MyWealth.Application.TenantAdmins;

public sealed class TenantAdminVm
{
    public int Id { get; init; }

    public int TenantId { get; init; }

    public required string TenantName { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }

    public bool IsEnabled { get; init; }
}
