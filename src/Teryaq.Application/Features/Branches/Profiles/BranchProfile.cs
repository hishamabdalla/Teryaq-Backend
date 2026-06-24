namespace Teryaq.Application.Features.Branches.Profiles;

using AutoMapper;
using Teryaq.Application.Features.Branches.Dtos;
using Teryaq.Domain.Features.Branches;

/// <summary>AutoMapper profile for <see cref="Branch"/> ↔ DTO mappings.</summary>
public sealed class BranchProfile : Profile
{
    /// <summary>Initialises a new instance of <see cref="BranchProfile"/>.</summary>
    public BranchProfile()
    {
        CreateMap<Branch, BranchDto>();
    }
}
