namespace Teryaq.Application.Features.Products.Validators;

using Teryaq.Application.Features.Products.Dtos;

/// <summary>Validates <see cref="CreateProductRequest"/> before product creation.</summary>
public sealed class CreateProductRequestValidator : ProductRequestValidatorBase<CreateProductRequest>
{
    /// <summary>Initialises a new instance of <see cref="CreateProductRequestValidator"/> with standard product rules.</summary>
    public CreateProductRequestValidator() =>
        ApplyProductRules(x => x.Name, x => x.Description, x => x.Price);
}
