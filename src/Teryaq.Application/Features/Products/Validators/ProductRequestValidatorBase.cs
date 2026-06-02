namespace Teryaq.Application.Features.Products.Validators;

using FluentValidation;

/// <summary>Common validation rules shared by <see cref="CreateProductRequestValidator"/> and <see cref="UpdateProductRequestValidator"/>.</summary>
/// <typeparam name="TRequest">The concrete request DTO type.</typeparam>
public abstract class ProductRequestValidatorBase<TRequest> : AbstractValidator<TRequest>
{
    /// <summary>Applies rules for <c>Name</c>, <c>Description</c>, and <c>Price</c> on <paramref name="nameSelector"/>, <paramref name="descriptionSelector"/>, and <paramref name="priceSelector"/>.</summary>
    /// <param name="nameSelector">Expression selecting the Name property.</param>
    /// <param name="descriptionSelector">Expression selecting the Description property.</param>
    /// <param name="priceSelector">Expression selecting the Price property.</param>
    protected void ApplyProductRules(
        System.Linq.Expressions.Expression<Func<TRequest, string>> nameSelector,
        System.Linq.Expressions.Expression<Func<TRequest, string?>> descriptionSelector,
        System.Linq.Expressions.Expression<Func<TRequest, decimal>> priceSelector)
    {
        RuleFor(nameSelector).NotEmpty().MaximumLength(200);
        RuleFor(descriptionSelector).MaximumLength(2000);
        RuleFor(priceSelector).GreaterThan(0);
    }
}
