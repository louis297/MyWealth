using MyWealth.Domain.Entities;

namespace MyWealth.Domain.Events;

public class UserDisabledEvent : BaseEvent
{
    public UserDisabledEvent(User user)
    {
        User = user;
    }

    public User User { get; }
}
