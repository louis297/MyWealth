using MyWealth.Domain.Entities;

namespace MyWealth.Domain.Events;

public class UserEnabledEvent : BaseEvent
{
    public UserEnabledEvent(User user)
    {
        User = user;
    }

    public User User { get; }
}
