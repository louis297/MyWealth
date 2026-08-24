using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
using MyWealth.Domain.ValueObjects;
using MyWealth.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MyWealth.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser
{
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            // See https://jasontaylor.dev/ef-core-database-initialisation-strategies
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        var tenant = await EnsureSampleTenantAsync();

        await EnsureLoginUserAsync(
            email: "sa1@localhost",
            password: "SystemAdmin1!",
            displayName: "System Admin1",
            role: UserRole.SystemAdmin,
            tenantId: null);

        await EnsureLoginUserAsync(
            email: "t1ta1@localhost",
            password: "TenantAdmin1!",
            displayName: "Tenant1 Admin1",
            role: UserRole.TenantAdmin,
            tenantId: tenant.Id);

        var adviser = await EnsureLoginUserAsync(
            email: "t1ad1@localhost",
            password: "Adviser1!",
            displayName: "Tenant1 Adviser1",
            role: UserRole.Adviser,
            tenantId: tenant.Id);

        var customer = await EnsureDomainCustomerAsync(
            tenantId: tenant.Id,
            adviserId: adviser.Id,
            email: "t1c1@localhost",
            name: "Tenant1 Customer1");

        var account = await EnsureSampleAccountAsync(customer);
        await EnsureSampleHoldingAsync(account);
    }

    private async Task<Tenant> EnsureSampleTenantAsync()
    {
        var existing = await _context.Tenants.FirstOrDefaultAsync();
        if (existing is not null)
        {
            return existing;
        }

        var tenant = Tenant.Create("Sample Tenant");
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        return tenant;
    }

    private async Task<User> EnsureLoginUserAsync(
        string email,
        string password,
        string displayName,
        UserRole role,
        int? tenantId)
    {
        var existing = await _context.DomainUsers.FirstOrDefaultAsync(u => u.Email == email);
        if (existing is not null)
        {
            return existing;
        }

        var user = role switch
        {
            UserRole.SystemAdmin => User.CreateSystemAdmin(displayName, email),
            UserRole.TenantAdmin => User.CreateTenantAdmin(tenantId!.Value, displayName, email),
            UserRole.Adviser => User.CreateAdviser(tenantId!.Value, displayName, email),
            _ => throw new InvalidOperationException($"Cannot seed a login user for role {role}.")
        };

        _context.DomainUsers.Add(user);
        await _context.SaveChangesAsync();

        var identityUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            Role = role,
            TenantId = tenantId,
            IsEnabled = true
        };

        var result = await _userManager.CreateAsync(identityUser, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        user.LinkIdentity(identityUser.Id);
        await _context.SaveChangesAsync();
        return user;
    }

    private async Task<User> EnsureDomainCustomerAsync(int tenantId, int adviserId, string email, string name)
    {
        var existing = await _context.DomainUsers.FirstOrDefaultAsync(u => u.Email == email);
        if (existing is not null)
        {
            return existing;
        }

        var customer = User.CreateCustomer(tenantId, adviserId, name, email);
        _context.DomainUsers.Add(customer);
        await _context.SaveChangesAsync();
        return customer;
    }

    private async Task<Account> EnsureSampleAccountAsync(User customer)
    {
        var existing = await _context.Accounts.FirstOrDefaultAsync(a => a.CustomerId == customer.Id);
        if (existing is not null)
        {
            return existing;
        }

        var account = Account.Open(
            customer.TenantId!.Value,
            customer.Id,
            "Primary Brokerage",
            AccountType.Brokerage,
            "NZD");
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    private async Task EnsureSampleHoldingAsync(Account account)
    {
        if (await _context.Holdings.AnyAsync(h => h.AccountId == account.Id))
        {
            return;
        }

        account.AddHolding(
            Instrument.Create("Apple Inc.", "AAPL"),
            100m,
            Money.Of(18500m, "NZD"));
        await _context.SaveChangesAsync();
    }
}
