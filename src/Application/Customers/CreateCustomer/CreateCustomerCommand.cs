using MyWealth.Application.Common.Exceptions;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;

namespace MyWealth.Application.Customers.CreateCustomer;

[Authorize(Roles = CustomerVisibility.AllowedRoles)]
public record CreateCustomerCommand : IRequest<int>
{
    public string? Name { get; init; }

    public string? Email { get; init; }

    public int AdviserId { get; init; }
}

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateCustomerCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<int> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        if (_user.TenantId is not int tenantId)
        {
            throw new ForbiddenAccessException();
        }

        var user = User.CreateCustomer(tenantId, request.AdviserId, request.Name!, request.Email!);
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
