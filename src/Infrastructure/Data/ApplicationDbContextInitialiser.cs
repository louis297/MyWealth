using MyWealth.Domain.Constants;
using MyWealth.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
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
    private readonly RoleManager<IdentityRole> _roleManager;

    public ApplicationDbContextInitialiser(
        ILogger<ApplicationDbContextInitialiser> logger,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
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
        await EnsureRoleAsync(Roles.SystemAdmin);
        await EnsureRoleAsync(Roles.TenantAdmin);
        await EnsureRoleAsync(Roles.Adviser);
        await EnsureRoleAsync(Roles.Customer);

        await EnsureUserAsync(
            email: "admin@localhost",
            password: "Administrator1!",
            displayName: "System Admin",
            role: Roles.SystemAdmin,
            tenantId: null);

        await EnsureUserAsync(
            email: "tenantadmin@localhost",
            password: "TenantAdmin1!",
            displayName: "Tenant Admin",
            role: Roles.TenantAdmin,
            tenantId: 1);

        await EnsureUserAsync(
            email: "adviser@localhost",
            password: "Adviser1!",
            displayName: "Adviser",
            role: Roles.Adviser,
            tenantId: 1);

        await EnsureUserAsync(
            email: "customer@localhost",
            password: "Customer1!",
            displayName: "Customer",
            role: Roles.Customer,
            tenantId: 1);
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        await _roleManager.CreateAsync(new IdentityRole(roleName));
    }

    private async Task EnsureUserAsync(
        string email,
        string password,
        string displayName,
        string role,
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
            TenantId = tenantId,
            IsEnabled = true
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to seed user '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        await _userManager.AddToRoleAsync(user, role);
    }
}
