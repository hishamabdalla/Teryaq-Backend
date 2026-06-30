namespace Teryaq.Application.Features.Customers.Profiles;

using AutoMapper;
using Teryaq.Application.Features.Customers.Dtos;
using Teryaq.Domain.Features.Customers;

/// <summary>AutoMapper profile for <see cref="Customer"/> projections.</summary>
public sealed class CustomerProfile : Profile
{
    /// <summary>Initialises a new instance of <see cref="CustomerProfile"/>.</summary>
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>();
    }
}
