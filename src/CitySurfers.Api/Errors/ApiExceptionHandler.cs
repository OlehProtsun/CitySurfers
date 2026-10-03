using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CitySurfers.Domain.Running;

namespace CitySurfers.Api.Errors;

internal sealed class ApiExceptionHandler(
    IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var rule = exception as RunRuleException;
        var status = rule?.Error switch
        {
            RunError.Validation => StatusCodes.Status400BadRequest,
            RunError.NotFound => StatusCodes.Status404NotFound,
            RunError.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        if (rule is null)
            logger.LogError("Request failed with {ExceptionType}. TraceId: {TraceId}",
                exception.GetType().Name, context.TraceIdentifier);
        context.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = rule?.Message ?? "An unexpected server error occurred."
        };
        if (!await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem
        }))
        {
            await context.Response.WriteAsJsonAsync(problem, options: null,
                contentType: "application/problem+json", cancellationToken: cancellationToken);
        }
        return true;
    }
}
