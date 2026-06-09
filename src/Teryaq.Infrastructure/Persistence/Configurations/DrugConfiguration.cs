namespace Teryaq.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Teryaq.Domain.Features.Drugs;

/// <summary>Configures the <see cref="Drug"/> entity schema using the EF Core Fluent API.</summary>
public sealed class DrugConfiguration : IEntityTypeConfiguration<Drug>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Drug> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(d => d.Id);

        builder.Property(d => d.TradeNameAr).IsRequired().HasMaxLength(500);
        builder.Property(d => d.TradeNameEn).IsRequired().HasMaxLength(500);
        builder.Property(d => d.GenericName).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(d => d.DosageForm).IsRequired().HasMaxLength(100);
        builder.Property(d => d.Strength).IsRequired().HasMaxLength(100);
        builder.Property(d => d.PackSize).IsRequired();
        builder.Property(d => d.Price).IsRequired().HasPrecision(18, 4);
        builder.Property(d => d.Barcode).HasMaxLength(50);
        builder.Property(d => d.ManufacturerAr).HasMaxLength(300);
        builder.Property(d => d.ManufacturerEn).HasMaxLength(300);

        builder.Property(d => d.Source)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(d => d.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(d => d.IsDeleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(d => d.Barcode)
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL");

        builder.HasIndex(d => new { d.TradeNameEn, d.Strength, d.DosageForm });

        builder.Ignore(d => d.DomainEvents);

        builder.HasQueryFilter(d => !d.IsDeleted);
    }
}
