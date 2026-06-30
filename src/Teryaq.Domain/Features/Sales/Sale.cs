namespace Teryaq.Domain.Features.Sales;

using Teryaq.Domain.Common;
using Teryaq.Domain.Features.Branches;
using Teryaq.Domain.Features.Customers;

/// <summary>Represents a completed POS sale transaction at a pharmacy branch.</summary>
public sealed class Sale : BaseEntity, ITenantEntity
{
    private readonly List<SaleLine> _lines = [];

    /// <inheritdoc/>
    public Guid TenantId { get; set; }

    /// <summary>Gets the identifier of the branch where the sale was made.</summary>
    public Guid BranchId { get; private set; }

    /// <summary>Gets the identifier of the user (cashier) who processed the sale.</summary>
    public Guid CashierUserId { get; private set; }

    /// <summary>Gets the human-readable sale / receipt number.</summary>
    public string SaleNumber { get; private set; } = string.Empty;

    /// <summary>Gets the sum of all line totals before discount.</summary>
    public decimal Total { get; private set; }

    /// <summary>Gets the manual discount applied to the entire sale.</summary>
    public decimal Discount { get; private set; }

    /// <summary>Gets the payment method used for this sale.</summary>
    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>Gets the current lifecycle status of the sale.</summary>
    public SaleStatus Status { get; private set; }

    /// <summary>Gets the UTC timestamp when the sale was finalised at the POS.</summary>
    public DateTime CompletedAt { get; private set; }

    /// <summary>Gets the optional identifier of the customer linked to this sale.</summary>
    public Guid? CustomerId { get; private set; }

    /// <summary>Gets the branch where this sale was made. Populated when the entity is loaded with includes.</summary>
    public Branch Branch { get; private set; } = null!;

    /// <summary>Gets the customer linked to this sale, or <see langword="null"/> for a walk-in sale. Populated when the entity is loaded with includes.</summary>
    public Customer? Customer { get; private set; }

    /// <summary>Gets the dispensed line items for this sale.</summary>
    public IReadOnlyList<SaleLine> Lines => _lines;

    /// <summary>Private constructor for EF Core materialisation.</summary>
    private Sale() { }

    /// <summary>Creates a new <see cref="Sale"/>.</summary>
    /// <param name="branchId">Branch where the sale occurs.</param>
    /// <param name="cashierUserId">User processing the sale.</param>
    /// <param name="saleNumber">Human-readable receipt number.</param>
    /// <param name="total">Sum of line totals before discount.</param>
    /// <param name="discount">Manual discount applied to the sale.</param>
    /// <param name="paymentMethod">Payment method used.</param>
    /// <param name="completedAt">UTC timestamp when the sale was finalised.</param>
    /// <param name="customerId">Optional customer linked to this sale.</param>
    public static Sale Create(
        Guid branchId,
        Guid cashierUserId,
        string saleNumber,
        decimal total,
        decimal discount,
        PaymentMethod paymentMethod,
        DateTime completedAt,
        Guid? customerId) =>
        new()
        {
            BranchId = branchId,
            CashierUserId = cashierUserId,
            SaleNumber = saleNumber,
            Total = total,
            Discount = discount,
            PaymentMethod = paymentMethod,
            Status = SaleStatus.Completed,
            CompletedAt = completedAt,
            CustomerId = customerId,
        };
}
