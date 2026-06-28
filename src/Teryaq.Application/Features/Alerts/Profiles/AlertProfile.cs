namespace Teryaq.Application.Features.Alerts.Profiles;

using AutoMapper;
using Teryaq.Application.Features.Alerts.Dtos;
using Teryaq.Domain.Features.Alerts;
using Teryaq.Domain.Features.Inventory;

/// <summary>AutoMapper profile that maps <see cref="StockBatch"/> to <see cref="AlertDto"/>.</summary>
public sealed class AlertProfile : Profile
{
    /// <summary>Initialises a new instance of <see cref="AlertProfile"/>.</summary>
    public AlertProfile()
    {
        // AlertDto is a positional record whose primary constructor has parameters
        // (Type, StockBatchId) that don't map 1-to-1 to StockBatch properties.
        // ConstructUsing explicitly drives the constructor call; Type and DaysUntilExpiry
        // are placeholder values that AlertService overrides via 'with' after mapping.
        CreateMap<StockBatch, AlertDto>()
            .ConstructUsing((src, _) => new AlertDto(
                AlertType.NearExpiry,
                AlertSeverity.Low,
                src.Id,
                src.BranchId,
                src.Branch != null ? src.Branch.Name : string.Empty,
                src.DrugId,
                src.Drug != null ? src.Drug.TradeNameEn : string.Empty,
                src.Drug != null ? src.Drug.TradeNameAr : string.Empty,
                src.BatchNumber,
                src.ExpiryDate,
                (int?)null,
                src.QuantityOnHand,
                src.ReorderLevel));
    }
}
