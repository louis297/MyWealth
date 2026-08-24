using MyWealth.Domain.Entities;

namespace MyWealth.Domain.Events;

public class AccountOpenedEvent : BaseEvent
{
    public AccountOpenedEvent(Account account)
    {
        Account = account;
    }

    public Account Account { get; }
}
