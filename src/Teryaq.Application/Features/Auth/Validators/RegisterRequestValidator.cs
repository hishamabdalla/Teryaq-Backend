namespace Teryaq.Application.Features.Auth.Validators;

using FluentValidation;
using Teryaq.Application.Features.Auth.Dtos;

/// <summary>Validates <see cref="RegisterRequest"/> before it reaches the service layer.</summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    /// <summary>Initialises a new instance of <see cref="RegisterRequestValidator"/>.</summary>
    public RegisterRequestValidator()
    {
        RuleFor(x => x.PharmacyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BranchName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BranchAddress).MaximumLength(500);
        RuleFor(x => x.BranchPhone).MaximumLength(20);
        RuleFor(x => x.OwnerEmail).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.OwnerPassword).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}
