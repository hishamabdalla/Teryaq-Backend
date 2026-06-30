namespace Teryaq.Application.Features.Sales.Dtos;

using Teryaq.Domain.Features.Sales;

/// <summary>Request payload to confirm a POS sale and dispense stock.</summary>
/// <param name="BranchId">Identifier of the branch where the sale is being made.</param>
/// <param name="Items">One or more drug line items in the sale. Must not be empty.</param>
/// <param name="Discount">Manual discount applied to the sale total. Use 0 for no discount.</param>
/// <param name="PaymentMethod">Payment method used by the customer.</param>
/// <param name="CustomerId">Optional identifier of the customer linked to this sale.</param>
public sealed record CreateSaleRequest(
    Guid BranchId,
    IReadOnlyList<SaleLineRequest> Items,
    decimal Discount,
    PaymentMethod PaymentMethod,
    Guid? CustomerId);
