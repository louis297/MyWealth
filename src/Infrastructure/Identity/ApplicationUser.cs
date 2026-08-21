using MyWealth.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace MyWealth.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public int? TenantId { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int? AdviserId { get; set; }
}
