using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Models;
using MyWealth.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace MyWealth.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
    private readonly IAuthorizationService _authorizationService;
    private readonly JwtTokenGenerator _jwtTokenGenerator;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
        IAuthorizationService authorizationService,
        JwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
        _authorizationService = authorizationService;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<string?> GetUserNameAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user?.UserName;
    }

    public async Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
            EmailConfirmed = true,
            DisplayName = userName,
            IsEnabled = true
        };

        var result = await _userManager.CreateAsync(user, password);

        return (result.ToApplicationResult(), user.Id);
    }

    public async Task<(Result Result, string UserId)> CreateLoginUserAsync(
        string email,
        string password,
        string displayName,
        UserRole role,
        int? tenantId,
        CancellationToken cancellationToken = default)
    {
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

        return (result.ToApplicationResult(), user.Id);
    }

    public async Task<Result> SetEnabledAsync(
        string userId,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Result.Failure(["User not found."]);
        }

        user.IsEnabled = isEnabled;

        var result = await _userManager.UpdateAsync(user);

        return result.ToApplicationResult();
    }

    public async Task<bool> IsInRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null && string.Equals(user.Role.ToString(), role, StringComparison.Ordinal);
    }

    public async Task<bool> AuthorizeAsync(string userId, string policyName)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return false;
        }

        var principal = await _userClaimsPrincipalFactory.CreateAsync(user);

        var result = await _authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    public async Task<Result> DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return user != null ? await DeleteUserAsync(user) : Result.Success();
    }

    public async Task<Result> DeleteUserAsync(ApplicationUser user)
    {
        var result = await _userManager.DeleteAsync(user);

        return result.ToApplicationResult();
    }

    public async Task<AuthenticationResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            return AuthenticationResult.Failed();
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);

        if (!passwordValid)
        {
            return AuthenticationResult.Failed();
        }

        if (user.Role == UserRole.Customer)
        {
            return AuthenticationResult.Customer();
        }

        if (!user.IsEnabled)
        {
            return AuthenticationResult.Disabled();
        }

        var (token, expiresIn) = _jwtTokenGenerator.CreateToken(user);

        return AuthenticationResult.Success(
            token,
            expiresIn,
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            user.Role.ToString(),
            user.TenantId);
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return null;
        }

        return new CurrentUserDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString(),
            TenantId = user.TenantId,
            IsEnabled = user.IsEnabled
        };
    }

    public async Task<Result> UpdateDisplayNameAsync(
        string userId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Result.Failure(["User not found."]);
        }

        user.DisplayName = displayName;

        var result = await _userManager.UpdateAsync(user);

        return result.ToApplicationResult();
    }

    public async Task<Result> ChangePasswordAsync(
        string userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return Result.Failure(["User not found."]);
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        return result.ToApplicationResult();
    }
}
