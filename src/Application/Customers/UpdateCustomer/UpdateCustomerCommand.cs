using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Entities;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;

namespace MyWealth.Application.Customers.UpdateCustomer;

[Authorize(Roles = CustomerVisibility.AllowedRoles)]
public record UpdateCustomerCommand : IRequest
{
    public int Id { get; init; }

    public string? Name { get; init; }

    public bool? IsEnabled { get; init; }

    public int? AdviserId { get; init; }
}

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateCustomerCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var user = await CustomerVisibility.FindVisibleCustomerAsync(
            _context, _user, request.Id, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }

        if (request.IsEnabled == false)
        {
            user.Disable();
        }
        else if (request.IsEnabled == true)
        {
            user.Enable();
        }

        if (request.Name is not null)
        {
            user.Rename(request.Name);
        }

        if (request.AdviserId is int adviserId)
        {
            user.ReassignAdviser(adviserId);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
