namespace Teryaq.Application.Features.Inventory.Profiles;

using AutoMapper;
using Teryaq.Application.Features.Inventory.Dtos;
using Teryaq.Domain.Features.Inventory;

/// <summary>AutoMapper profile that maps <see cref="StockBatch"/> to <see cref="StockBatchDto"/>.</summary>
public sealed class StockBatchProfile : Profile
{
    /// <summary>Initialises a new instance of <see cref="StockBatchProfile"/>.</summary>
    public StockBatchProfile()
    {
        CreateMap<StockBatch, StockBatchDto>()
            .ForMember(d => d.BranchName, o => o.MapFrom(s => s.Branch != null ? s.Branch.Name : string.Empty))
            .ForMember(d => d.DrugTradeNameEn, o => o.MapFrom(s => s.Drug != null ? s.Drug.TradeNameEn : string.Empty))
            .ForMember(d => d.DrugTradeNameAr, o => o.MapFrom(s => s.Drug != null ? s.Drug.TradeNameAr : string.Empty));
    }
}
