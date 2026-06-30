namespace Teryaq.Application.Features.Sales.Validators;

using FluentValidation;
using Teryaq.Application.Features.Sales.Dtos;

/// <summary>Validates <see cref="CreateSaleRequest"/> before the sale is processed.</summary>
public sealed class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    /// <summary>Initialises a new instance of <see cref="CreateSaleRequestValidator"/>.</summary>
    public CreateSaleRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("A sale must contain at least one line item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DrugId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });

        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
    }
}
