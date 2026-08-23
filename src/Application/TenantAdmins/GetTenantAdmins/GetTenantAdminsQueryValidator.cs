namespace MyWealth.Application.TenantAdmins.GetTenantAdmins;

public class GetTenantAdminsQueryValidator : AbstractValidator<GetTenantAdminsQuery>
{
    public GetTenantAdminsQueryValidator()
    {
        RuleFor(v => v.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(v => v.PageSize)
            .InclusiveBetween(1, 100);

        When(v => v.TenantId is not null, () =>
        {
            RuleFor(v => v.TenantId)
                .GreaterThan(0);
        });
    }
}
