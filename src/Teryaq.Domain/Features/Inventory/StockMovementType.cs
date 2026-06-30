namespace Teryaq.Domain.Features.Inventory;

/// <summary>Describes the direction and reason for a stock quantity change.</summary>
public enum StockMovementType
{
    /// <summary>Units were physically received into the branch.</summary>
    Receive,

    /// <summary>Units were dispensed at the POS.</summary>
    Dispense,

    /// <summary>Quantity was manually adjusted (e.g. after a physical count).</summary>
    Adjust,
}
