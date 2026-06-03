namespace Teryaq.Application.Features.Auth.Validators;

using FluentValidation;
using Teryaq.Application.Features.Auth.Dtos;

/// <summary>Validates <see cref="LoginRequest"/> before it reaches the service layer.</summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>Initialises a new instance of <see cref="LoginRequestValidator"/>.</summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
