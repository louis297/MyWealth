namespace MyWealth.Domain.ValueObjects;

public class Instrument : ValueObject
{
    public const int NameMaxLength = 200;
    public const int SymbolMaxLength = 50;

    public string Name { get; private set; } = null!;

    public string? Symbol { get; private set; }

    private Instrument()
    {
    }

    private Instrument(string name, string? symbol)
    {
        Name = name;
        Symbol = symbol;
    }

    public static Instrument Create(string name, string? symbol = null)
    {
        return new Instrument(NormaliseName(name), NormaliseSymbol(symbol));
    }

    private static string NormaliseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            throw new ArgumentException($"Name must be {NameMaxLength} characters or fewer.", nameof(name));
        }

        return trimmed;
    }

    private static string? NormaliseSymbol(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        var trimmed = symbol.Trim();
        if (trimmed.Length > SymbolMaxLength)
        {
            throw new ArgumentException($"Symbol must be {SymbolMaxLength} characters or fewer.", nameof(symbol));
        }

        return trimmed;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Symbol ?? string.Empty;
    }
}
