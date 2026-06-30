namespace Teryaq.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teryaq.Domain.Features.Sales;

/// <summary>Configures the <see cref="SaleLine"/> entity schema using the EF Core Fluent API.</summary>
public sealed class SaleLineConfiguration : IEntityTypeConfiguration<SaleLine>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SaleLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(l => l.Id);

        builder.Property(l => l.TenantId).IsRequired();

        builder.Property(l => l.SaleId).IsRequired();

        builder.Property(l => l.DrugId).IsRequired();

        builder.Property(l => l.BatchId).IsRequired();

        builder.Property(l => l.Quantity).IsRequired();

        builder.Property(l => l.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(l => l.LineTotal)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(l => l.IsDeleted).IsRequired().HasDefaultValue(false);

        // Tenant filter + soft-delete filter applied centrally in AppDbContext.OnModelCreating
        // for all ITenantEntity types — do NOT add HasQueryFilter here.

        builder.HasOne(l => l.Drug)
            .WithMany()
            .HasForeignKey(l => l.DrugId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Batch)
            .WithMany()
            .HasForeignKey(l => l.BatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Teryaq.Domain.Features.Tenants.Tenant>()
            .WithMany()
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(l => l.DomainEvents);
    }
}
