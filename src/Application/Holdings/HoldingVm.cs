namespace MyWealth.Application.Holdings;

public sealed class HoldingVm
{
    public int Id { get; init; }

    public int AccountId { get; init; }

    public required InstrumentVm Instrument { get; init; }

    public decimal Quantity { get; init; }

    public required MoneyVm CostBasis { get; init; }
}

public sealed class InstrumentVm
{
    public required string Name { get; init; }

    public string? Symbol { get; init; }
}

public sealed class MoneyVm
{
    public decimal Amount { get; init; }

    public required string Currency { get; init; }
}

public sealed class InstrumentDto
{
    public string? Name { get; init; }

    public string? Symbol { get; init; }
}

public sealed class MoneyDto
{
    public decimal? Amount { get; init; }

    public string? Currency { get; init; }
}
