namespace Teryaq.Application.Features.Products.Validators;

using Teryaq.Application.Features.Products.Dtos;

/// <summary>Validates <see cref="UpdateProductRequest"/> before product replacement.</summary>
public sealed class UpdateProductRequestValidator : ProductRequestValidatorBase<UpdateProductRequest>
{
    /// <summary>Initialises a new instance of <see cref="UpdateProductRequestValidator"/> with standard product rules.</summary>
    public UpdateProductRequestValidator() =>
        ApplyProductRules(x => x.Name, x => x.Description, x => x.Price);
}
