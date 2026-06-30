namespace Teryaq.Application.Features.Sales.Dtos;

using Teryaq.Domain.Features.Sales;

/// <summary>Full sale detail including all dispensed line items.</summary>
/// <param name="Id">Sale identifier.</param>
/// <param name="BranchId">Identifier of the branch where the sale was made.</param>
/// <param name="BranchName">Name of the branch.</param>
/// <param name="SaleNumber">Human-readable receipt / sale number.</param>
/// <param name="CashierUserId">Identifier of the cashier who processed the sale.</param>
/// <param name="Total">Sum of all line totals before discount.</param>
/// <param name="Discount">Manual discount applied to the sale.</param>
/// <param name="GrandTotal">Amount the customer paid (Total − Discount).</param>
/// <param name="PaymentMethod">Payment method used.</param>
/// <param name="Status">Current lifecycle status of the sale.</param>
/// <param name="CompletedAt">UTC timestamp when the sale was finalised.</param>
/// <param name="CustomerId">Optional identifier of the linked customer.</param>
/// <param name="CustomerName">Display name of the linked customer, or <see langword="null"/>.</param>
/// <param name="Lines">All dispensed line items in this sale.</param>
/// <param name="CreatedAt">UTC timestamp when the record was created.</param>
public sealed record SaleDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    string SaleNumber,
    Guid CashierUserId,
    decimal Total,
    decimal Discount,
    decimal GrandTotal,
    PaymentMethod PaymentMethod,
    SaleStatus Status,
    DateTime CompletedAt,
    Guid? CustomerId,
    string? CustomerName,
    List<SaleLineDto> Lines,
    DateTime CreatedAt);
