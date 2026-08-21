using System.Security.Claims;
using MyWealth.Application.Common.Interfaces;
using MyWealth.Application.Common.Security;
using Microsoft.AspNetCore.Http;

namespace MyWealth.Application.FunctionalTests.Infrastructure;

public sealed class TestUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TestUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Id =>
        TestApp.GetUserId() ??
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public IReadOnlyList<string>? Roles =>
        TestApp.GetRoles() ??
        _httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(x => x.Value).ToList();

    public int? TenantId
    {
        get
        {
            if (TestApp.GetUserId() is not null)
            {
                return TestApp.GetTenantId();
            }

            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue(CustomClaims.TenantId);

            return int.TryParse(value, out var tenantId) ? tenantId : null;
        }
    }
}
