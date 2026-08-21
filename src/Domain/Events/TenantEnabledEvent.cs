using MyWealth.Domain.Entities;

namespace MyWealth.Domain.Events;

public class TenantEnabledEvent : BaseEvent
{
    public TenantEnabledEvent(Tenant tenant)
    {
        Tenant = tenant;
    }

    public Tenant Tenant { get; }
}
