namespace MyWealth.Application.Advisers;

public sealed class AdviserVm
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }

    public bool IsEnabled { get; init; }
}
