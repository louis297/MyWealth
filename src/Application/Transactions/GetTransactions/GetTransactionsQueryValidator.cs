namespace MyWealth.Application.Transactions.GetTransactions;

public class GetTransactionsQueryValidator : AbstractValidator<GetTransactionsQuery>
{
    public GetTransactionsQueryValidator()
    {
        RuleFor(v => v.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(v => v.PageSize)
            .InclusiveBetween(1, 100);

        When(v => v.AccountId is not null, () =>
        {
            RuleFor(v => v.AccountId!.Value)
                .GreaterThan(0)
                .OverridePropertyName(nameof(GetTransactionsQuery.AccountId));
        });

        When(v => v.Type is not null, () =>
        {
            RuleFor(v => v.Type)
                .IsInEnum();
        });

        When(v => v.From is not null && v.To is not null, () =>
        {
            RuleFor(v => v)
                .Must(v => v.From <= v.To)
                .WithMessage("'from' must be on or before 'to'.")
                .OverridePropertyName(nameof(GetTransactionsQuery.From));
        });
    }
}
