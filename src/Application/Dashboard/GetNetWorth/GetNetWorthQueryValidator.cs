namespace MyWealth.Application.Dashboard.GetNetWorth;

public class GetNetWorthQueryValidator : AbstractValidator<GetNetWorthQuery>
{
    public GetNetWorthQueryValidator()
    {
        When(v => v.CustomerId is not null, () =>
        {
            RuleFor(v => v.CustomerId!.Value)
                .GreaterThan(0)
                .OverridePropertyName(nameof(GetNetWorthQuery.CustomerId));
        });
    }
}
