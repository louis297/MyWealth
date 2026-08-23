using System.Reflection;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Domain.Entities;
using MyWealth.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MyWealth.Infrastructure.Data;

public class ApplicationDbContext : IdentityUserContext<ApplicationUser>, IApplicationDbContext
{
    private readonly IUser _user;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUser user) : base(options)
    {
        _user = user;
    }

    public int? CurrentTenantId => _user.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> DomainUsers => Set<User>();

    DbSet<User> IApplicationDbContext.Users => DomainUsers;

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
        await operation(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        builder.Entity<User>()
            .HasQueryFilter(u => CurrentTenantId == null || u.TenantId == CurrentTenantId);
    }
}
