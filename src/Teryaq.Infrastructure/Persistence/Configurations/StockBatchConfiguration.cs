namespace Teryaq.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teryaq.Domain.Features.Inventory;

/// <summary>Configures the <see cref="StockBatch"/> entity schema using the EF Core Fluent API.</summary>
public sealed class StockBatchConfiguration : IEntityTypeConfiguration<StockBatch>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<StockBatch> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(b => b.Id);

        builder.Property(b => b.TenantId).IsRequired();

        builder.Property(b => b.BranchId).IsRequired();

        builder.Property(b => b.DrugId).IsRequired();

        builder.Property(b => b.BatchNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.ExpiryDate).IsRequired();

        builder.Property(b => b.QuantityReceived).IsRequired();

        builder.Property(b => b.QuantityOnHand).IsRequired();

        builder.Property(b => b.ReorderLevel)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(b => b.CostPrice)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(b => b.SellingPrice)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(b => b.ReceivedAt).IsRequired();

        builder.Property(b => b.IsDeleted).IsRequired().HasDefaultValue(false);

        // Tenant filter + soft-delete filter applied centrally in AppDbContext.OnModelCreating
        // for all ITenantEntity types — do NOT add HasQueryFilter here.

        builder.HasIndex(b => b.TenantId);
        builder.HasIndex(b => new { b.BranchId, b.ExpiryDate });
        builder.HasIndex(b => new { b.BranchId, b.QuantityOnHand });
        builder.HasIndex(b => b.DrugId);

        builder.HasOne(b => b.Branch)
            .WithMany()
            .HasForeignKey(b => b.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Drug)
            .WithMany()
            .HasForeignKey(b => b.DrugId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Teryaq.Domain.Features.Tenants.Tenant>()
            .WithMany()
            .HasForeignKey(b => b.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(b => b.DomainEvents);
    }
}
