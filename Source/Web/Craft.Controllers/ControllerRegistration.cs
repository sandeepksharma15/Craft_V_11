using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Craft.Controllers;

public static class ControllerRegistration
{
    public static IMvcBuilder AddCraftControllers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddProblemDetails();
        services.AddExceptionHandler<CraftControllerExceptionHandler>();
        return services.AddControllers();
    }
}

internal sealed class CraftControllerExceptionHandler(ILogger<CraftControllerExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested) return false;
        var concurrency = exception is DbUpdateConcurrencyException;
        var status = concurrency ? StatusCodes.Status412PreconditionFailed : StatusCodes.Status500InternalServerError;
        logger.LogError(exception, "Controller request failed with status {StatusCode}; trace {TraceId}", status, context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = concurrency ? "The entity changed while the request was being processed." : "An unexpected error occurred.",
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
