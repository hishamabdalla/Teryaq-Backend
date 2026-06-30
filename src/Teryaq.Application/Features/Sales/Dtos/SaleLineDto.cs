namespace Teryaq.Application.Features.Sales.Dtos;

/// <summary>Represents a single dispensed line item within a sale response.</summary>
/// <param name="Id">Line item identifier.</param>
/// <param name="DrugId">Identifier of the drug dispensed.</param>
/// <param name="DrugTradeNameEn">English trade name of the drug.</param>
/// <param name="DrugTradeNameAr">Arabic trade name of the drug.</param>
/// <param name="BatchId">Identifier of the stock batch that was decremented.</param>
/// <param name="BatchNumber">Supplier lot number of the dispensed batch.</param>
/// <param name="Quantity">Number of units dispensed.</param>
/// <param name="UnitPrice">Per-unit selling price at the time of sale.</param>
/// <param name="LineTotal">Total for this line (Quantity × UnitPrice).</param>
public sealed record SaleLineDto(
    Guid Id,
    Guid DrugId,
    string DrugTradeNameEn,
    string DrugTradeNameAr,
    Guid BatchId,
    string BatchNumber,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
