using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using MyWealth.Domain.Constants;
using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using NotFoundException = MyWealth.Application.Common.Exceptions.NotFoundException;

namespace MyWealth.Application.Advisers.GetAdviserById;

[Authorize(Roles = Roles.TenantAdmin)]
public record GetAdviserByIdQuery : IRequest<AdviserVm>
{
    public int Id { get; init; }
}

public class GetAdviserByIdQueryHandler : IRequestHandler<GetAdviserByIdQuery, AdviserVm>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetAdviserByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<AdviserVm> Handle(GetAdviserByIdQuery request, CancellationToken cancellationToken)
    {
        var adviser = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.Id && u.Role == UserRole.Adviser)
            .ProjectTo<AdviserVm>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (adviser is null)
        {
            throw new NotFoundException(nameof(User), request.Id);
        }

        return adviser;
    }
}
