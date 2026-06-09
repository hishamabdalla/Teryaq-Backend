namespace Teryaq.API.Controllers.Base;

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Teryaq.Application.Common;

/// <summary>Base controller that wires <see cref="Result{TValue}"/> outcomes to HTTP responses.</summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion(1)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Maps a service result to the appropriate 2xx or 4xx response.</summary>
    /// <param name="result">The result returned by the service.</param>
    protected IActionResult HandleResult<TValue>(Result<TValue> result) =>
        result.IsSuccess ? Ok(result.Value) : MapErrorToResponse(result.Error);

    /// <summary>Returns 201 Created on success, or delegates to <see cref="HandleResult{TValue}"/> on failure.</summary>
    /// <param name="result">The result returned by the service.</param>
    /// <param name="actionName">Name of the GET action used to build the <c>Location</c> header.</param>
    /// <param name="routeValues">Route values forwarded to <c>CreatedAtAction</c>.</param>
    protected IActionResult HandleCreated<TValue>(Result<TValue> result, string actionName, object routeValues) =>
        result.IsSuccess ? CreatedAtAction(actionName, routeValues, result.Value) : MapErrorToResponse(result.Error);

    /// <summary>Returns 204 No Content on success, or a 4xx problem response on failure.</summary>
    /// <param name="result">The result returned by the service.</param>
    protected IActionResult HandleDelete(Result result) =>
        result.IsSuccess ? NoContent() : MapErrorToResponse(result.Error);

    private ObjectResult MapErrorToResponse(ResultError error) => error.Code switch
    {
        ResultErrorCodes.NotFound => NotFound(CreateProblem(404, error.Title, error.Message)),
        ResultErrorCodes.Conflict => Conflict(CreateProblem(409, error.Title, error.Message)),
        ResultErrorCodes.Forbidden => StatusCode(StatusCodes.Status403Forbidden, CreateProblem(403, error.Title, error.Message)),
        ResultErrorCodes.Validation => UnprocessableEntity(CreateValidationProblem(error)),
        _ => BadRequest(CreateProblem(400, error.Title, error.Message)),
    };

    private static ValidationProblemDetails CreateValidationProblem(ResultError error)
    {
        var details = new ValidationProblemDetails(
            error.ValidationErrors?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            ?? new Dictionary<string, string[]>())
        {
            Status = 422,
            Detail = error.Message,
        };
        return details;
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
    };
}
