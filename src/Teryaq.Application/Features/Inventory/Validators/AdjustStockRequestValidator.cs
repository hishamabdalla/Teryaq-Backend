namespace Teryaq.Application.Features.Inventory.Validators;

using FluentValidation;
using Teryaq.Application.Features.Inventory.Dtos;

/// <summary>Validates <see cref="AdjustStockRequest"/> before it reaches the service layer.</summary>
public sealed class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    /// <summary>Initialises a new instance of <see cref="AdjustStockRequestValidator"/>.</summary>
    public AdjustStockRequestValidator()
    {
        RuleFor(x => x.QuantityOnHand).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReorderLevel).GreaterThanOrEqualTo(0);
    }
}
