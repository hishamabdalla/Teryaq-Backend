namespace Teryaq.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teryaq.Domain.Features.Inventory;

/// <summary>Configures the <see cref="StockMovement"/> entity schema using the EF Core Fluent API.</summary>
public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TenantId).IsRequired();

        builder.Property(m => m.BranchId).IsRequired();

        builder.Property(m => m.DrugId).IsRequired();

        builder.Property(m => m.BatchId).IsRequired();

        builder.Property(m => m.Type).IsRequired();

        builder.Property(m => m.Quantity).IsRequired();

        builder.Property(m => m.UserId).IsRequired();

        builder.Property(m => m.Reason).HasMaxLength(500);

        builder.Property(m => m.IsDeleted).IsRequired().HasDefaultValue(false);

        // Tenant filter + soft-delete filter applied centrally in AppDbContext.OnModelCreating
        // for all ITenantEntity types — do NOT add HasQueryFilter here.

        builder.HasIndex(m => new { m.TenantId, m.BatchId, m.Type });

        builder.HasOne<Teryaq.Domain.Features.Inventory.StockBatch>()
            .WithMany()
            .HasForeignKey(m => m.BatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Teryaq.Domain.Features.Tenants.Tenant>()
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(m => m.DomainEvents);
    }
}
