namespace MyWealth.Application.Accounts.GetAccounts;

public class GetAccountsQueryValidator : AbstractValidator<GetAccountsQuery>
{
    public GetAccountsQueryValidator()
    {
        RuleFor(v => v.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(v => v.PageSize)
            .InclusiveBetween(1, 100);

        When(v => v.Status is not null, () =>
        {
            RuleFor(v => v.Status)
                .IsInEnum();
        });

        When(v => v.CustomerId is not null, () =>
        {
            RuleFor(v => v.CustomerId!.Value)
                .GreaterThan(0)
                .OverridePropertyName(nameof(GetAccountsQuery.CustomerId));
        });
    }
}
