namespace Teryaq.Application.Features.Sales.Dtos;

/// <summary>Lightweight summary of a single sale shown in the today's-sales list.</summary>
/// <param name="Id">Sale identifier.</param>
/// <param name="SaleNumber">Human-readable receipt / sale number.</param>
/// <param name="GrandTotal">Amount paid by the customer (Total − Discount).</param>
/// <param name="ItemCount">Total number of line items in the sale.</param>
/// <param name="CompletedAt">UTC timestamp when the sale was finalised.</param>
/// <param name="CustomerName">Display name of the linked customer, or <see langword="null"/> for a walk-in sale.</param>
public sealed record TodaySaleSummaryDto(
    Guid Id,
    string SaleNumber,
    decimal GrandTotal,
    int ItemCount,
    DateTime CompletedAt,
    string? CustomerName);
