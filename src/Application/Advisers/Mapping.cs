using MyWealth.Domain.Entities;

namespace MyWealth.Application.Advisers;

public class Mapping : Profile
{
    public Mapping()
    {
        CreateMap<User, AdviserVm>();
    }
}
