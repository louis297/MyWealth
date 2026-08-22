using MyWealth.Domain.Entities;
using MyWealth.Domain.Enums;
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

        await EnsureUserAsync(
            email: "sa1@localhost",
            password: "SystemAdmin1!",
            displayName: "System Admin1",
            role: UserRole.SystemAdmin,
            tenantId: null);

        await EnsureUserAsync(
            email: "t1ta1@localhost",
            password: "TenantAdmin1!",
            displayName: "Tenant1 Admin1",
            role: UserRole.TenantAdmin,
            tenantId: tenant.Id);

        await EnsureUserAsync(
            email: "t1ad1@localhost",
            password: "Adviser1!",
            displayName: "Tenant1 Adviser1",
            role: UserRole.Adviser,
            tenantId: tenant.Id);

        await EnsureUserAsync(
            email: "t1c1@localhost",
            password: "Customer1!",
            displayName: "Tenant1 Customer1",
            role: UserRole.Customer,
            tenantId: tenant.Id);
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

    private async Task EnsureUserAsync(
        string email,
        string password,
        string displayName,
        UserRole role,
        int? tenantId)
    {
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            Role = role,
            TenantId = tenantId,
            IsEnabled = true
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}
