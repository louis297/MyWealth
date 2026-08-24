namespace MyWealth.Application.Dashboard.GetAssetAllocation;

public class GetAssetAllocationQueryValidator : AbstractValidator<GetAssetAllocationQuery>
{
    public GetAssetAllocationQueryValidator()
    {
        When(v => v.CustomerId is not null, () =>
        {
            RuleFor(v => v.CustomerId!.Value)
                .GreaterThan(0)
                .OverridePropertyName(nameof(GetAssetAllocationQuery.CustomerId));
        });
    }
}
