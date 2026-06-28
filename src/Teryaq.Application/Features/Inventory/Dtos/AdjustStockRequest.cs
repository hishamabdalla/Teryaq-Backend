namespace Teryaq.Application.Features.Inventory.Dtos;

/// <summary>Payload for applying a correction to an existing stock batch.</summary>
/// <param name="QuantityOnHand">Corrected quantity on hand (e.g. after a physical count) — must be non-negative.</param>
/// <param name="SellingPrice">Updated per-unit selling price — must be non-negative.</param>
/// <param name="ExpiryDate">Corrected expiry date.</param>
/// <param name="ReorderLevel">Updated minimum on-hand threshold for low-stock alerts; use 0 to disable.</param>
public sealed record AdjustStockRequest(
    int QuantityOnHand,
    decimal SellingPrice,
    DateOnly ExpiryDate,
    int ReorderLevel);
