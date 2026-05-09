namespace CatalogAPI.API.Middlewares;

public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Store in HttpContext.Items for access in controllers/handlers
        context.Items["CorrelationId"] = correlationId;
        
        // Add to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId.ToString());
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static Guid GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationIdString) 
            && Guid.TryParse(correlationIdString, out var correlationId))
        {
            return correlationId;
        }

        return Guid.NewGuid();
    }
}
