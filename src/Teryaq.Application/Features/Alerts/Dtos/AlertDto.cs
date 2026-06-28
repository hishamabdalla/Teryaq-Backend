namespace Teryaq.Application.Features.Alerts.Dtos;

using Teryaq.Domain.Features.Alerts;

/// <summary>Read model for a near-expiry or low-stock alert raised against a stock batch.</summary>
/// <param name="Type">The kind of alert — <see cref="AlertType.NearExpiry"/> or <see cref="AlertType.LowStock"/>.</param>
/// <param name="Severity">Urgency level — <see cref="AlertSeverity.High"/>, <see cref="AlertSeverity.Medium"/>, or <see cref="AlertSeverity.Low"/>.</param>
/// <param name="StockBatchId">Identifier of the flagged stock batch.</param>
/// <param name="BranchId">Identifier of the branch holding the batch.</param>
/// <param name="BranchName">Display name of the holding branch.</param>
/// <param name="DrugId">Identifier of the drug in the global catalog.</param>
/// <param name="DrugTradeNameEn">English brand name of the drug.</param>
/// <param name="DrugTradeNameAr">Arabic brand name of the drug.</param>
/// <param name="BatchNumber">Supplier lot number.</param>
/// <param name="ExpiryDate">Expiry date of this lot.</param>
/// <param name="DaysUntilExpiry">Number of days until the batch expires; <see langword="null"/> for <see cref="AlertType.LowStock"/> alerts.</param>
/// <param name="QuantityOnHand">Current units on hand.</param>
/// <param name="ReorderLevel">Configured minimum on-hand threshold.</param>
public sealed record AlertDto(
    AlertType Type,
    AlertSeverity Severity,
    Guid StockBatchId,
    Guid BranchId,
    string BranchName,
    Guid DrugId,
    string DrugTradeNameEn,
    string DrugTradeNameAr,
    string BatchNumber,
    DateOnly ExpiryDate,
    int? DaysUntilExpiry,
    int QuantityOnHand,
    int ReorderLevel);
