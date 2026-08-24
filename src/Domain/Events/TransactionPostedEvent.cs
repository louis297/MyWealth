using MyWealth.Domain.Entities;

namespace MyWealth.Domain.Events;

public class TransactionPostedEvent : BaseEvent
{
    public TransactionPostedEvent(Account account, Transaction transaction)
    {
        Account = account;
        Transaction = transaction;
    }

    public Account Account { get; }

    public Transaction Transaction { get; }
}
