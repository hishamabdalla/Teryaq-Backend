namespace Teryaq.Application.Common.Tenancy;

/// <summary>Resolves the authenticated tenant and branch identifiers from the current request context.</summary>
public interface ICurrentTenant
{
    /// <summary>Gets the current tenant identifier, or <see cref="Guid.Empty"/> when there is no authenticated HTTP context (e.g. background jobs).</summary>
    Guid TenantId { get; }

    /// <summary>Gets the current branch identifier, or <see langword="null"/> when the token carries no branch claim.</summary>
    Guid? BranchId { get; }
}
