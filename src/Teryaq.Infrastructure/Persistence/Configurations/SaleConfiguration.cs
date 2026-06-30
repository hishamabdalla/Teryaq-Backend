namespace Teryaq.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teryaq.Domain.Features.Sales;

/// <summary>Configures the <see cref="Sale"/> entity schema using the EF Core Fluent API.</summary>
public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId).IsRequired();

        builder.Property(s => s.BranchId).IsRequired();

        builder.Property(s => s.CashierUserId).IsRequired();

        builder.Property(s => s.SaleNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(s => s.Total)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(s => s.Discount)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(s => s.PaymentMethod).IsRequired();

        builder.Property(s => s.Status).IsRequired();

        builder.Property(s => s.CompletedAt).IsRequired();

        builder.Property(s => s.IsDeleted).IsRequired().HasDefaultValue(false);

        // Tenant filter + soft-delete filter applied centrally in AppDbContext.OnModelCreating
        // for all ITenantEntity types — do NOT add HasQueryFilter here.

        builder.HasIndex(s => new { s.TenantId, s.CompletedAt });
        builder.HasIndex(s => s.SaleNumber);

        builder.HasOne(s => s.Branch)
            .WithMany()
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        // CashierUserId is an audit field only — no hard FK to ApplicationUser so that
        // sales records survive if the cashier account is later removed.

        builder.HasOne(s => s.Customer)
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Teryaq.Domain.Features.Tenants.Tenant>()
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lines)
            .WithOne()
            .HasForeignKey(l => l.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.DomainEvents);
    }
}
