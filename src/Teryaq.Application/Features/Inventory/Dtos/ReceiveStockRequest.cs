namespace Teryaq.Application.Features.Inventory.Dtos;

/// <summary>Payload for receiving a new stock batch into a branch's inventory.</summary>
/// <param name="BranchId">Identifier of the branch receiving the stock.</param>
/// <param name="DrugId">Identifier of the drug in the global catalog.</param>
/// <param name="BatchNumber">Supplier lot number (required, max 100 characters).</param>
/// <param name="ExpiryDate">Expiry date of this lot — must be a future date.</param>
/// <param name="Quantity">Number of units received — must be at least 1.</param>
/// <param name="ReorderLevel">Minimum on-hand quantity before a low-stock alert is raised. Use 0 to disable alerts for this batch.</param>
/// <param name="CostPrice">Purchase cost per unit in Egyptian pounds — must be non-negative.</param>
/// <param name="SellingPrice">Per-unit selling price; defaults to the drug's catalog price when <see langword="null"/>.</param>
public sealed record ReceiveStockRequest(
    Guid BranchId,
    Guid DrugId,
    string BatchNumber,
    DateOnly ExpiryDate,
    int Quantity,
    int ReorderLevel,
    decimal CostPrice,
    decimal? SellingPrice);
