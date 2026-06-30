namespace Teryaq.Domain.Features.Inventory;

using Teryaq.Domain.Common;

/// <summary>Immutable audit record of every quantity change to a stock batch.</summary>
public sealed class StockMovement : BaseEntity, ITenantEntity
{
    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    /// <summary>Gets the identifier of the branch where the movement occurred.</summary>
    public Guid BranchId { get; private set; }

    /// <summary>Gets the identifier of the drug whose stock changed.</summary>
    public Guid DrugId { get; private set; }

    /// <summary>Gets the identifier of the stock batch that was modified.</summary>
    public Guid BatchId { get; private set; }

    /// <summary>Gets the type of stock movement.</summary>
    public StockMovementType Type { get; private set; }

    /// <summary>Gets the number of units moved (always positive; direction is conveyed by <see cref="Type"/>).</summary>
    public int Quantity { get; private set; }

    /// <summary>Gets the identifier of the user who triggered the movement.</summary>
    public Guid UserId { get; private set; }

    /// <summary>Gets the identifier of the related business document (e.g. <c>Sale.Id</c> for <see cref="StockMovementType.Dispense"/> movements), or <see langword="null"/> if not applicable.</summary>
    public Guid? ReferenceId { get; private set; }

    /// <summary>Gets a free-text reason for the movement, or <see langword="null"/> if none was provided.</summary>
    public string? Reason { get; private set; }

    /// <summary>Private constructor for EF Core materialisation.</summary>
    private StockMovement() { }

    /// <summary>Creates a new <see cref="StockMovement"/>.</summary>
    /// <param name="branchId">Branch where the movement occurred.</param>
    /// <param name="drugId">Drug whose stock changed.</param>
    /// <param name="batchId">Batch that was modified.</param>
    /// <param name="type">Type of movement.</param>
    /// <param name="quantity">Units moved (positive integer).</param>
    /// <param name="userId">User who triggered the movement.</param>
    /// <param name="referenceId">Optional reference to the triggering business document.</param>
    /// <param name="reason">Optional free-text reason.</param>
    public static StockMovement Create(
        Guid branchId,
        Guid drugId,
        Guid batchId,
        StockMovementType type,
        int quantity,
        Guid userId,
        Guid? referenceId,
        string? reason) =>
        new()
        {
            BranchId = branchId,
            DrugId = drugId,
            BatchId = batchId,
            Type = type,
            Quantity = quantity,
            UserId = userId,
            ReferenceId = referenceId,
            Reason = reason,
        };
}
