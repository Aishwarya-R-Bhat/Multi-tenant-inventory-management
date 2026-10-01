using InventoryPlatform.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace InventoryPlatform.Api.Infrastructure;

/// <summary>Turns exceptions into RFC 7807 ProblemDetails responses so every error has the same shape.</summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var problem = ex switch
            {
                ValidationFailedException v => new ValidationProblemDetails(v.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation failed"
                },
                UnauthorizedException u => new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = u.Message },
                ConflictException c => new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = c.Message },
                _ => new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "An unexpected error occurred." }
            };

            if (problem.Status == StatusCodes.Status500InternalServerError)
                logger.LogError(ex, "Unhandled exception");

            problem.Extensions["correlationId"] = context.TraceIdentifier;
            context.Response.StatusCode = problem.Status!.Value;
            await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
    }
}
