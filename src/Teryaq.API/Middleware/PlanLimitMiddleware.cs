namespace Teryaq.API.Middleware;

/// <summary>
/// Stub middleware that will enforce per-plan feature limits (branch count, user count, etc.)
/// in Phase 1. Currently passes all requests through without restriction.
/// </summary>
public sealed class PlanLimitMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Initialises a new instance of <see cref="PlanLimitMiddleware"/>.</summary>
    public PlanLimitMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Invokes the middleware; plan-limit enforcement will be wired in Phase 1.</summary>
    public Task InvokeAsync(HttpContext context) => _next(context);
}
