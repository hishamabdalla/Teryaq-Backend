namespace Teryaq.Application.Features.Sales.Profiles;

using AutoMapper;
using Teryaq.Application.Features.Sales.Dtos;
using Teryaq.Domain.Features.Sales;

/// <summary>AutoMapper profile for <see cref="Sale"/> and <see cref="SaleLine"/> projections.</summary>
public sealed class SaleProfile : Profile
{
    /// <summary>Initialises a new instance of <see cref="SaleProfile"/>.</summary>
    public SaleProfile()
    {
        CreateMap<SaleLine, SaleLineDto>()
            .ConstructUsing((src, _) => new SaleLineDto(
                src.Id,
                src.DrugId,
                src.Drug != null ? src.Drug.TradeNameEn : string.Empty,
                src.Drug != null ? src.Drug.TradeNameAr : string.Empty,
                src.BatchId,
                src.Batch != null ? src.Batch.BatchNumber : string.Empty,
                src.Quantity,
                src.UnitPrice,
                src.LineTotal));

        CreateMap<Sale, SaleDto>()
            .ConstructUsing((src, ctx) => new SaleDto(
                src.Id,
                src.BranchId,
                src.Branch != null ? src.Branch.Name : string.Empty,
                src.SaleNumber,
                src.CashierUserId,
                src.Total,
                src.Discount,
                src.Total - src.Discount,
                src.PaymentMethod,
                src.Status,
                src.CompletedAt,
                src.CustomerId,
                src.Customer != null ? src.Customer.Name : null,
                ctx.Mapper.Map<List<SaleLineDto>>(src.Lines),
                src.CreatedAt));

        CreateMap<Sale, TodaySaleSummaryDto>()
            .ConstructUsing((src, _) => new TodaySaleSummaryDto(
                src.Id,
                src.SaleNumber,
                src.Total - src.Discount,
                src.Lines.Count,
                src.CompletedAt,
                src.Customer != null ? src.Customer.Name : null));
    }
}
