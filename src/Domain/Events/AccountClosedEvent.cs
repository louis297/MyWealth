using MyWealth.Domain.Entities;

namespace MyWealth.Domain.Events;

public class AccountClosedEvent : BaseEvent
{
    public AccountClosedEvent(Account account)
    {
        Account = account;
    }

    public Account Account { get; }
}
