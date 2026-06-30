namespace Teryaq.Infrastructure.Services;

using Microsoft.AspNetCore.Http;
using Teryaq.Application.Common.Tenancy;

/// <summary>Resolves tenant and branch identifiers from the current JWT claims.</summary>
public sealed class CurrentTenantService : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initialises a new instance of <see cref="CurrentTenantService"/>.</summary>
    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public Guid TenantId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id");
            return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;
        }
    }

    /// <inheritdoc/>
    public Guid? BranchId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("branch_id");
            return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
        }
    }

    /// <inheritdoc/>
    public Guid? UserId
    {
        get
        {
            // JsonWebTokenHandler (default in .NET 8+) keeps "sub" as-is without mapping it
            // to ClaimTypes.NameIdentifier, so we look up the raw JWT claim name directly.
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("sub");
            return claim is not null && Guid.TryParse(claim.Value, out var id) ? id : null;
        }
    }
}
