using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;

namespace MyWealth.Application.Customers.DisableCustomer;

[Authorize(Roles = CustomerVisibility.AllowedRoles)]
public record DisableCustomerCommand : IRequest
{
    public int Id { get; init; }
}

public class DisableCustomerCommandHandler : IRequestHandler<DisableCustomerCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public DisableCustomerCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(DisableCustomerCommand request, CancellationToken cancellationToken)
    {
        var user = await CustomerVisibility.FindVisibleCustomerAsync(
            _context, _user, request.Id, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }

        user.Disable();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
