using MyWealth.Domain.Constants;
using MyWealth.Infrastructure.Data;
using MyWealth.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MyWealth.Application.FunctionalTests.Infrastructure;

public static class TestApp
{
    private static string? _userId;
    private static List<string>? _roles;
    private static int? _tenantId;

    public static HttpClient CreateClient() => FunctionalTestSetup.Factory.CreateClient();

    public static async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        return await mediator.Send(request);
    }

    public static async Task SendAsync(IBaseRequest request)
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();

        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        await mediator.Send(request);
    }

    public static string? GetUserId() => _userId;

    public static List<string>? GetRoles() => _roles;

    public static int? GetTenantId() => _tenantId;

    public static async Task<string> RunAsDefaultUserAsync()
    {
        return await RunAsUserAsync("test@local", "Testing1234!", []);
    }

    public static async Task<string> RunAsSystemAdminAsync()
    {
        return await RunAsUserAsync("admin@local", "Administrator1!", [Roles.SystemAdmin]);
    }

    public static async Task<string> RunAsUserAsync(
        string userName,
        string password,
        string[] roles,
        int? tenantId = null,
        string? displayName = null,
        bool isEnabled = true)
    {
        var user = await CreateUserAsync(userName, password, roles, tenantId, displayName, isEnabled);

        _userId = user.Id;
        _roles = [..roles];
        _tenantId = tenantId;

        return user.Id;
    }

    public static async Task<ApplicationUser> CreateUserAsync(
        string userName,
        string password,
        string[] roles,
        int? tenantId = null,
        string? displayName = null,
        bool isEnabled = true)
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
            EmailConfirmed = true,
            DisplayName = displayName ?? userName,
            TenantId = tenantId,
            IsEnabled = isEnabled
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join(Environment.NewLine, result.ToApplicationResult().Errors);
            throw new Exception($"Unable to create {userName}.{Environment.NewLine}{errors}");
        }

        if (roles.Length > 0)
        {
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            await userManager.AddToRolesAsync(user, roles);
        }

        return user;
    }

    public static async Task ResetState()
    {
        if (FunctionalTestSetup.DbResetter is not null)
        {
            await FunctionalTestSetup.DbResetter.ResetAsync();
        }

        _userId = null;
        _roles = null;
        _tenantId = null;
    }

    public static async Task<TEntity?> FindAsync<TEntity>(params object[] keyValues)
        where TEntity : class
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.FindAsync<TEntity>(keyValues);
    }

    public static async Task AddAsync<TEntity>(TEntity entity)
        where TEntity : class
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.Add(entity);

        await context.SaveChangesAsync();
    }

    public static async Task<int> CountAsync<TEntity>() where TEntity : class
    {
        using var scope = FunctionalTestSetup.ScopeFactory.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Set<TEntity>().CountAsync();
    }
}
