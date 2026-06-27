namespace Teryaq.Domain.Features.Inventory;

using Teryaq.Domain.Common;
using Teryaq.Domain.Features.Branches;
using Teryaq.Domain.Features.Drugs;

/// <summary>Represents one received lot of a drug held in a branch's inventory.</summary>
public sealed class StockBatch : BaseEntity, ITenantEntity
{
    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    /// <summary>Gets the identifier of the branch that holds this batch.</summary>
    public Guid BranchId { get; private set; }

    /// <summary>Gets the identifier of the drug in the global catalog.</summary>
    public Guid DrugId { get; private set; }

    /// <summary>Gets the supplier lot number for this batch.</summary>
    public string BatchNumber { get; private set; } = string.Empty;

    /// <summary>Gets the expiry date of this lot.</summary>
    public DateOnly ExpiryDate { get; private set; }

    /// <summary>Gets the original quantity received — an immutable record of the lot size.</summary>
    public int QuantityReceived { get; private set; }

    /// <summary>Gets the current number of units on hand; decremented by POS dispensing.</summary>
    public int QuantityOnHand { get; private set; }

    /// <summary>Gets the purchase cost per unit in Egyptian pounds.</summary>
    public decimal CostPrice { get; private set; }

    /// <summary>Gets the per-unit selling price in Egyptian pounds.</summary>
    public decimal SellingPrice { get; private set; }

    /// <summary>Gets the UTC timestamp when the batch was physically received by the pharmacy.</summary>
    public DateTime ReceivedAt { get; private set; }

    /// <summary>Gets the drug from the global catalog. Populated when the entity is loaded with includes.</summary>
    public Drug Drug { get; private set; } = null!;

    /// <summary>Gets the branch that holds this batch. Populated when the entity is loaded with includes.</summary>
    public Branch Branch { get; private set; } = null!;

    /// <summary>Private constructor for EF Core materialisation.</summary>
    private StockBatch() { }

    /// <summary>Creates a new <see cref="StockBatch"/> for a received lot of drug inventory.</summary>
    /// <param name="branchId">Identifier of the receiving branch.</param>
    /// <param name="drugId">Identifier of the drug in the shared catalog.</param>
    /// <param name="batchNumber">Supplier lot number.</param>
    /// <param name="expiryDate">Expiry date of this lot.</param>
    /// <param name="quantity">Number of units received.</param>
    /// <param name="costPrice">Purchase cost per unit.</param>
    /// <param name="sellingPrice">Per-unit selling price.</param>
    /// <param name="receivedAt">UTC timestamp when the lot was physically received.</param>
    public static StockBatch Create(
        Guid branchId,
        Guid drugId,
        string batchNumber,
        DateOnly expiryDate,
        int quantity,
        decimal costPrice,
        decimal sellingPrice,
        DateTime receivedAt) =>
        new()
        {
            BranchId = branchId,
            DrugId = drugId,
            BatchNumber = batchNumber,
            ExpiryDate = expiryDate,
            QuantityReceived = quantity,
            QuantityOnHand = quantity,
            CostPrice = costPrice,
            SellingPrice = sellingPrice,
            ReceivedAt = receivedAt,
        };

    /// <summary>Applies corrections to on-hand quantity, selling price, or expiry date.</summary>
    /// <param name="quantityOnHand">Corrected quantity on hand (e.g. after a physical count).</param>
    /// <param name="sellingPrice">Updated per-unit selling price.</param>
    /// <param name="expiryDate">Corrected expiry date.</param>
    public void Adjust(int quantityOnHand, decimal sellingPrice, DateOnly expiryDate)
    {
        QuantityOnHand = quantityOnHand;
        SellingPrice = sellingPrice;
        ExpiryDate = expiryDate;
    }

    /// <summary>Decrements on-hand quantity when units are dispensed at the POS.</summary>
    /// <param name="quantity">Number of units dispensed.</param>
    public void Dispense(int quantity) => QuantityOnHand -= quantity;
}
