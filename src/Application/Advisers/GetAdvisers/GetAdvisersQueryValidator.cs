namespace MyWealth.Application.Advisers.GetAdvisers;

public class GetAdvisersQueryValidator : AbstractValidator<GetAdvisersQuery>
{
    public GetAdvisersQueryValidator()
    {
        RuleFor(v => v.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(v => v.PageSize)
            .InclusiveBetween(1, 100);
    }
}
