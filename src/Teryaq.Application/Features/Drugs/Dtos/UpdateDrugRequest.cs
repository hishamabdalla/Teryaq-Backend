namespace Teryaq.Application.Features.Drugs.Dtos;

/// <summary>Payload for a full replacement update of an existing drug. <c>Source</c> is intentionally excluded — it is immutable.</summary>
/// <param name="TradeNameAr">Arabic brand name.</param>
/// <param name="TradeNameEn">English brand name.</param>
/// <param name="GenericName">Active substance name.</param>
/// <param name="DosageForm">Dosage form (e.g. Tablet, Syrup).</param>
/// <param name="Strength">Dosage strength (e.g. 500mg).</param>
/// <param name="PackSize">Units per package.</param>
/// <param name="Price">EDA official public price in Egyptian pounds.</param>
/// <param name="Barcode">Optional EDA barcode.</param>
/// <param name="ManufacturerAr">Optional manufacturer name in Arabic.</param>
/// <param name="ManufacturerEn">Optional manufacturer name in English.</param>
/// <param name="IsActive">Whether this drug should be available for use.</param>
public sealed record UpdateDrugRequest(
    string TradeNameAr,
    string TradeNameEn,
    string GenericName,
    string DosageForm,
    string Strength,
    int PackSize,
    decimal Price,
    string? Barcode,
    string? ManufacturerAr,
    string? ManufacturerEn,
    bool IsActive);
