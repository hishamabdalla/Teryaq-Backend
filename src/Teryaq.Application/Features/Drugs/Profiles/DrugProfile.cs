namespace Teryaq.Application.Features.Drugs.Profiles;

using AutoMapper;
using Teryaq.Application.Features.Drugs.Dtos;
using Teryaq.Domain.Features.Drugs;

/// <summary>AutoMapper profile that maps <see cref="Drug"/> to <see cref="DrugDto"/>.</summary>
public sealed class DrugProfile : Profile
{
    /// <summary>Initialises a new instance of <see cref="DrugProfile"/>.</summary>
    public DrugProfile()
    {
        CreateMap<Drug, DrugDto>();
    }
}
