namespace MyWealth.Application.Common.Interfaces;

public interface IUser
{
    string? Id { get; }

    IReadOnlyList<string>? Roles { get; }

    int? TenantId { get; }
}
