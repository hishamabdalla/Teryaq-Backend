namespace Teryaq.Domain.Common;

/// <summary>Marks an entity as tenant-scoped. The infrastructure layer stamps <see cref="TenantId"/> on insert and applies a global query filter.</summary>
public interface ITenantEntity
{
    /// <summary>Gets or sets the identifier of the tenant that owns this entity.</summary>
    Guid TenantId { get; set; }
}
