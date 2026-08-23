namespace MyWealth.Application.Customers;

public sealed class CustomerVm
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }

    public bool IsEnabled { get; init; }

    public int AdviserId { get; init; }

    public required string AdviserName { get; init; }
}
