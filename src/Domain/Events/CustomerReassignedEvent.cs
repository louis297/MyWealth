using MyWealth.Domain.Entities;

namespace MyWealth.Domain.Events;

public class CustomerReassignedEvent : BaseEvent
{
    public CustomerReassignedEvent(User user)
    {
        User = user;
    }

    public User User { get; }
}
