namespace Teryaq.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teryaq.Domain.Features.Branches;

/// <summary>Configures the <see cref="Branch"/> entity schema using the EF Core Fluent API.</summary>
public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(b => b.Id);

        builder.Property(b => b.TenantId).IsRequired();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Address).HasMaxLength(500);

        builder.Property(b => b.Phone).HasMaxLength(20);

        builder.Property(b => b.IsMain).IsRequired().HasDefaultValue(false);

        builder.Property(b => b.IsActive).IsRequired().HasDefaultValue(true);

        builder.Property(b => b.IsDeleted).IsRequired().HasDefaultValue(false);

        // Tenant filter + soft-delete filter applied centrally in AppDbContext.OnModelCreating
        // for all ITenantEntity types — do NOT add HasQueryFilter here.

        builder.HasIndex(b => b.TenantId);

        builder.HasOne<Teryaq.Domain.Features.Tenants.Tenant>()
            .WithMany()
            .HasForeignKey(b => b.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(b => b.DomainEvents);
    }
}
