namespace Teryaq.Application.Features.Drugs.Dtos;

using Teryaq.Domain.Features.Drugs;

/// <summary>Read-only projection of a <see cref="Drug"/> returned by the API.</summary>
/// <param name="Id">Unique identifier.</param>
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
/// <param name="Source">How this drug was added to the catalog.</param>
/// <param name="IsActive">Whether this drug is available for use.</param>
/// <param name="CreatedAt">UTC timestamp when the drug was created.</param>
/// <param name="UpdatedAt">UTC timestamp of the last update, or <see langword="null"/> if never updated.</param>
public sealed record DrugDto(
    Guid Id,
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
    DrugSource Source,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
