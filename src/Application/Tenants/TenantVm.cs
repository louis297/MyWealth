namespace MyWealth.Application.Tenants;

public sealed class TenantVm
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public bool IsEnabled { get; init; }
}
