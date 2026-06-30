namespace Teryaq.Application.Features.Sales.Dtos;

/// <summary>Specifies a single drug line item within a <see cref="CreateSaleRequest"/>.</summary>
/// <param name="DrugId">Identifier of the drug to dispense.</param>
/// <param name="Quantity">Number of units to dispense. Must be greater than zero.</param>
public sealed record SaleLineRequest(Guid DrugId, int Quantity);
