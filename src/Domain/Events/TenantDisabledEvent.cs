using MyWealth.Domain.Entities;

namespace MyWealth.Domain.Events;

public class TenantDisabledEvent : BaseEvent
{
    public TenantDisabledEvent(Tenant tenant)
    {
        Tenant = tenant;
    }

    public Tenant Tenant { get; }
}
