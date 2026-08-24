using MyWealth.Application.Holdings;
using MyWealth.Domain.Entities;

namespace MyWealth.Application.Transactions;

internal static class TransactionMappings
{
    public static IQueryable<TransactionVm> ProjectToVm(IQueryable<Transaction> transactions)
        => transactions.Select(t => new TransactionVm
        {
            Id = t.Id,
            AccountId = t.AccountId,
            HoldingId = t.HoldingId,
            BookedOn = t.BookedOn,
            Type = t.Type,
            Amount = new MoneyVm
            {
                Amount = t.Amount.Amount,
                Currency = t.Amount.Currency
            },
            Quantity = t.Quantity,
            Note = t.Note
        });
}
