using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;

namespace MyWealth.Application.Customers.GetCustomerById;

[Authorize(Roles = CustomerVisibility.AllowedRoles)]
public record GetCustomerByIdQuery : IRequest<CustomerVm>
{
    public int Id { get; init; }
}

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetCustomerByIdQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<CustomerVm> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var adviserDomainUserId = CustomerVisibility.IsAdviser(_user)
            ? await CustomerVisibility.GetCallerDomainUserIdAsync(_context, _user, cancellationToken)
            : null;

        var customers = CustomerVisibility.ScopedCustomers(
            _context.Users.AsNoTracking(), _user, adviserDomainUserId);

        var customer = await CustomerVisibility
            .ProjectToVm(customers.Where(u => u.Id == request.Id), _context.Users.AsNoTracking())
            .FirstOrDefaultAsync(cancellationToken);

        if (customer is null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }

        return customer;
    }
}
