namespace Teryaq.Domain.Features.Sales;

/// <summary>Lifecycle state of a POS sale.</summary>
public enum SaleStatus
{
    /// <summary>The sale was completed and stock was dispensed.</summary>
    Completed,

    /// <summary>The sale was voided after creation; no stock change is reversed automatically.</summary>
    Voided,
}
