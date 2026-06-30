namespace Teryaq.Application.Features.Customers.Validators;

using FluentValidation;
using Teryaq.Application.Features.Customers.Dtos;

/// <summary>Validates <see cref="CreateCustomerRequest"/> before the customer is registered.</summary>
public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    /// <summary>Initialises a new instance of <see cref="CreateCustomerRequestValidator"/>.</summary>
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .When(x => x.Phone is not null);

        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(256)
            .When(x => x.Email is not null);
    }
}
