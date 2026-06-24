namespace Teryaq.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teryaq.Domain.Features.Branches;
using Teryaq.Domain.Features.Tenants;
using Teryaq.Infrastructure.Identity;

/// <summary>Configures the <see cref="ApplicationUser"/> schema extensions beyond the default Identity mappings.</summary>
public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200)
            .HasDefaultValue(string.Empty);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(u => u.BranchId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
