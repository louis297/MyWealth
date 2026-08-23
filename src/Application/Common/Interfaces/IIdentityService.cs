using MyWealth.Application.Common.Models;
using MyWealth.Domain.Enums;

namespace MyWealth.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<bool> AuthorizeAsync(string userId, string policyName);

    Task<(Result Result, string UserId)> CreateUserAsync(string userName, string password);

    Task<(Result Result, string UserId)> CreateLoginUserAsync(
        string email,
        string password,
        string displayName,
        UserRole role,
        int? tenantId,
        CancellationToken cancellationToken = default);

    Task<Result> SetEnabledAsync(string userId, bool isEnabled, CancellationToken cancellationToken = default);

    Task<Result> DeleteUserAsync(string userId);

    Task<AuthenticationResult> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<CurrentUserDto?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken = default);

    Task<Result> UpdateDisplayNameAsync(string userId, string displayName, CancellationToken cancellationToken = default);

    Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}
