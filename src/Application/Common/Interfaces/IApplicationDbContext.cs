using MyWealth.Domain.Entities;

namespace MyWealth.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }

    DbSet<User> Users { get; }

    DbSet<Account> Accounts { get; }

    DbSet<Holding> Holdings { get; }

    DbSet<Transaction> Transactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken);
}
