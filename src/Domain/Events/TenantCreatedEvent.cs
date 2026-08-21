using MyWealth.Domain.Entities;

namespace MyWealth.Domain.Events;

public class TenantCreatedEvent : BaseEvent
{
    public TenantCreatedEvent(Tenant tenant)
    {
        Tenant = tenant;
    }

    public Tenant Tenant { get; }
}
