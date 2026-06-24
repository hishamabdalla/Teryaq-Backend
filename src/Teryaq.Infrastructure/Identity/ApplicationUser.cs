namespace Teryaq.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;

/// <summary>Application user extending ASP.NET Core Identity with tenant and branch membership.</summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Gets or sets the display name for this user.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant this user belongs to.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the branch this user is assigned to, or <see langword="null"/> for owner-level users.</summary>
    public Guid? BranchId { get; set; }

    /// <summary>Gets or sets the opaque refresh token currently issued to this user.</summary>
    public string? RefreshToken { get; set; }

    /// <summary>Gets or sets the UTC expiry of the current refresh token.</summary>
    public DateTime? RefreshTokenExpiry { get; set; }
}
