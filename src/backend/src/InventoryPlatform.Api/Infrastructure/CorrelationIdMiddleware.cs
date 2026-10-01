using Serilog.Context;

namespace InventoryPlatform.Api.Infrastructure;

/// <summary>Reuses the caller's X-Correlation-Id or creates one, returns it in the response and adds it to every log line.</summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers[Header].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(id)) id = Guid.NewGuid().ToString("N");

        context.TraceIdentifier = id;
        context.Response.Headers[Header] = id;
        using (LogContext.PushProperty("CorrelationId", id))
            await next(context);
    }
}
