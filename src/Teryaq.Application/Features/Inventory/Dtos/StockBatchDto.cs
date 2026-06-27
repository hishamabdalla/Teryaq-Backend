namespace Teryaq.Application.Features.Inventory.Dtos;

using Teryaq.Domain.Features.Inventory;

/// <summary>Read-only projection of a <see cref="StockBatch"/> returned by the API.</summary>
/// <param name="Id">Unique identifier of the batch.</param>
/// <param name="BranchId">Identifier of the branch holding this batch.</param>
/// <param name="BranchName">Display name of the holding branch.</param>
/// <param name="DrugId">Identifier of the drug in the global catalog.</param>
/// <param name="DrugTradeNameEn">English brand name of the drug.</param>
/// <param name="DrugTradeNameAr">Arabic brand name of the drug.</param>
/// <param name="BatchNumber">Supplier lot number.</param>
/// <param name="ExpiryDate">Expiry date of this lot.</param>
/// <param name="QuantityReceived">Original quantity received — immutable record.</param>
/// <param name="QuantityOnHand">Current units on hand.</param>
/// <param name="CostPrice">Purchase cost per unit in Egyptian pounds.</param>
/// <param name="SellingPrice">Per-unit selling price in Egyptian pounds.</param>
/// <param name="ReceivedAt">UTC timestamp when the batch was physically received.</param>
/// <param name="CreatedAt">UTC timestamp when the record was created.</param>
/// <param name="UpdatedAt">UTC timestamp of the last update, or <see langword="null"/> if never updated.</param>
public sealed record StockBatchDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    Guid DrugId,
    string DrugTradeNameEn,
    string DrugTradeNameAr,
    string BatchNumber,
    DateOnly ExpiryDate,
    int QuantityReceived,
    int QuantityOnHand,
    decimal CostPrice,
    decimal SellingPrice,
    DateTime ReceivedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
