namespace Teryaq.Domain.Features.Sales;

using Teryaq.Domain.Common;
using Teryaq.Domain.Features.Drugs;
using Teryaq.Domain.Features.Inventory;

/// <summary>Represents a single dispensed item within a <see cref="Sale"/>.</summary>
public sealed class SaleLine : BaseEntity, ITenantEntity
{
    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    /// <summary>Gets the identifier of the parent sale.</summary>
    public Guid SaleId { get; private set; }

    /// <summary>Gets the identifier of the drug that was dispensed.</summary>
    public Guid DrugId { get; private set; }

    /// <summary>Gets the identifier of the specific stock batch that was decremented.</summary>
    public Guid BatchId { get; private set; }

    /// <summary>Gets the number of units dispensed.</summary>
    public int Quantity { get; private set; }

    /// <summary>Gets the per-unit selling price at the time of sale.</summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>Gets the total for this line (Quantity × UnitPrice).</summary>
    public decimal LineTotal { get; private set; }

    /// <summary>Gets the drug from the global catalog. Populated when the entity is loaded with includes.</summary>
    public Drug Drug { get; private set; } = null!;

    /// <summary>Gets the stock batch that was dispensed. Populated when the entity is loaded with includes.</summary>
    public StockBatch Batch { get; private set; } = null!;

    /// <summary>Private constructor for EF Core materialisation.</summary>
    private SaleLine() { }

    /// <summary>Creates a new <see cref="SaleLine"/>.</summary>
    /// <param name="saleId">Identifier of the parent sale.</param>
    /// <param name="drugId">Identifier of the drug dispensed.</param>
    /// <param name="batchId">Identifier of the stock batch decremented.</param>
    /// <param name="quantity">Number of units dispensed.</param>
    /// <param name="unitPrice">Per-unit price at time of sale.</param>
    public static SaleLine Create(
        Guid saleId,
        Guid drugId,
        Guid batchId,
        int quantity,
        decimal unitPrice) =>
        new()
        {
            SaleId = saleId,
            DrugId = drugId,
            BatchId = batchId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineTotal = quantity * unitPrice,
        };
}
