namespace Teryaq.Application.Features.Products.Profiles;

using AutoMapper;
using Teryaq.Application.Features.Products.Dtos;
using Teryaq.Domain.Features.Products;

/// <summary>AutoMapper profile that maps <see cref="Product"/> to <see cref="ProductDto"/>.</summary>
public sealed class ProductProfile : Profile
{
    /// <summary>Initialises a new instance of <see cref="ProductProfile"/> with the product mapping.</summary>
    public ProductProfile() => CreateMap<Product, ProductDto>();
}
