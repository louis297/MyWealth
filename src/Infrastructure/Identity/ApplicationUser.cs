using Microsoft.AspNetCore.Identity;

namespace MyWealth.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public int? TenantId { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int? AdviserId { get; set; }
}
