using MyWealth.Domain.Entities;

namespace MyWealth.Domain.Events;

public class HoldingChangedEvent : BaseEvent
{
    public HoldingChangedEvent(Account account, Holding holding)
    {
        Account = account;
        Holding = holding;
    }

    public Account Account { get; }

    public Holding Holding { get; }
}
