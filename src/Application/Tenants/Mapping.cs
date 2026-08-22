using MyWealth.Domain.Entities;

namespace MyWealth.Application.Tenants;

public class Mapping : Profile
{
    public Mapping()
    {
        CreateMap<Tenant, TenantVm>();
    }
}
