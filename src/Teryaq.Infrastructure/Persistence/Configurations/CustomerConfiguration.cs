namespace Teryaq.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teryaq.Domain.Features.Customers;

/// <summary>Configures the <see cref="Customer"/> entity schema using the EF Core Fluent API.</summary>
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId).IsRequired();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Phone).HasMaxLength(20);

        builder.Property(c => c.Email).HasMaxLength(256);

        builder.Property(c => c.IsDeleted).IsRequired().HasDefaultValue(false);

        // Tenant filter + soft-delete filter applied centrally in AppDbContext.OnModelCreating
        // for all ITenantEntity types — do NOT add HasQueryFilter here.

        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => c.Phone);

        builder.HasOne<Teryaq.Domain.Features.Tenants.Tenant>()
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(c => c.DomainEvents);
    }
}
