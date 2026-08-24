namespace MyWealth.Domain.ValueObjects;

public class Money : ValueObject
{
    public const int CurrencyLength = 3;

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = null!;

    private Money()
    {
    }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Of(decimal amount, string currency)
    {
        return new Money(amount, NormaliseCurrency(currency));
    }

    public Money WithAmount(decimal amount) => new(amount, Currency);

    internal static string NormaliseCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        var normalised = currency.Trim().ToUpperInvariant();
        if (normalised.Length != CurrencyLength || !normalised.All(char.IsAsciiLetter))
        {
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));
        }

        return normalised;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
