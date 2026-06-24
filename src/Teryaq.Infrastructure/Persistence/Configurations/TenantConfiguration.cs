namespace Teryaq.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teryaq.Domain.Features.Tenants;

/// <summary>Configures the <see cref="Tenant"/> entity schema using the EF Core Fluent API.</summary>
public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Plan)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.OnboardingStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.ContactEmail).HasMaxLength(256);

        builder.Property(t => t.ContactPhone).HasMaxLength(20);

        builder.Property(t => t.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasQueryFilter(t => !t.IsDeleted);

        builder.HasIndex(t => t.Name);

        builder.Ignore(t => t.DomainEvents);
    }
}
