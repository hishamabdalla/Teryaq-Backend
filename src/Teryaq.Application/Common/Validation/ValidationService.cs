namespace Teryaq.Application.Common.Validation;

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Teryaq.Application.Common;

/// <inheritdoc cref="IValidationService"/>
public sealed partial class ValidationService : IValidationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ValidationService> _logger;

    /// <summary>Initialises a new instance of <see cref="ValidationService"/>.</summary>
    public ValidationService(IServiceProvider serviceProvider, ILogger<ValidationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> ValidateAsync<TRequest>(TRequest request, CancellationToken ct = default)
    {
        var validator = _serviceProvider.GetService<IValidator<TRequest>>();

        if (validator is null)
        {
            Log.NoValidatorRegistered(_logger, typeof(TRequest).Name);
            return Result.Ok();
        }

        var validationResult = await validator.ValidateAsync(request, ct);
        if (validationResult.IsValid)
        {
            return Result.Ok();
        }

        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return Result.Fail(ResultError.Validation(errors));
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Warning, Message = "No IValidator<{RequestType}> registered. Validation was skipped.")]
        public static partial void NoValidatorRegistered(ILogger logger, string requestType);
    }
}
