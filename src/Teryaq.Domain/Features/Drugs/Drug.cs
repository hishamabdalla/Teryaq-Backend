namespace Teryaq.Domain.Features.Drugs;

using Teryaq.Domain.Common;

/// <summary>A drug entry in the global shared catalog. Not tenant-scoped.</summary>
public sealed class Drug : BaseEntity
{
    /// <summary>Gets the Arabic brand (trade) name.</summary>
    public string TradeNameAr { get; private set; } = string.Empty;

    /// <summary>Gets the English brand (trade) name.</summary>
    public string TradeNameEn { get; private set; } = string.Empty;

    /// <summary>Gets the generic (active substance) name.</summary>
    public string GenericName { get; private set; } = string.Empty;

    /// <summary>Gets the dosage form (e.g. Tablet, Syrup, Injection).</summary>
    public string DosageForm { get; private set; } = string.Empty;

    /// <summary>Gets the dosage strength (e.g. 500mg, 250mg/5ml).</summary>
    public string Strength { get; private set; } = string.Empty;

    /// <summary>Gets the number of units per package.</summary>
    public int PackSize { get; private set; }

    /// <summary>Gets the EDA official public price in Egyptian pounds.</summary>
    public decimal Price { get; private set; }

    /// <summary>Gets the optional EDA barcode.</summary>
    public string? Barcode { get; private set; }

    /// <summary>Gets the optional manufacturer name in Arabic.</summary>
    public string? ManufacturerAr { get; private set; }

    /// <summary>Gets the optional manufacturer name in English.</summary>
    public string? ManufacturerEn { get; private set; }

    /// <summary>Gets the source that identifies how this drug was added to the catalog.</summary>
    public DrugSource Source { get; private set; }

    /// <summary>Gets a value indicating whether this drug is available for use in inventory and POS.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Private constructor for EF Core materialisation.</summary>
    private Drug() { }

    /// <summary>Creates a new <see cref="Drug"/> in the catalog.</summary>
    public static Drug Create(
        string tradeNameAr,
        string tradeNameEn,
        string genericName,
        string dosageForm,
        string strength,
        int packSize,
        decimal price,
        string? barcode,
        string? manufacturerAr,
        string? manufacturerEn,
        DrugSource source) =>
        new()
        {
            TradeNameAr = tradeNameAr,
            TradeNameEn = tradeNameEn,
            GenericName = genericName,
            DosageForm = dosageForm,
            Strength = strength,
            PackSize = packSize,
            Price = price,
            Barcode = barcode,
            ManufacturerAr = manufacturerAr,
            ManufacturerEn = manufacturerEn,
            Source = source,
            IsActive = true,
        };

    /// <summary>Replaces all mutable fields. <see cref="Source"/> is intentionally excluded — it is set at creation and never changes.</summary>
    public void Update(
        string tradeNameAr,
        string tradeNameEn,
        string genericName,
        string dosageForm,
        string strength,
        int packSize,
        decimal price,
        string? barcode,
        string? manufacturerAr,
        string? manufacturerEn,
        bool isActive)
    {
        TradeNameAr = tradeNameAr;
        TradeNameEn = tradeNameEn;
        GenericName = genericName;
        DosageForm = dosageForm;
        Strength = strength;
        PackSize = packSize;
        Price = price;
        Barcode = barcode;
        ManufacturerAr = manufacturerAr;
        ManufacturerEn = manufacturerEn;
        IsActive = isActive;
    }
}
