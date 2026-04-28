namespace NICE.Platform.Collaboration.API.Middleware;
using NICE.Platform.Collaboration.Application.Interfaces.Repositories;

public class ApiKeyMiddleware(RequestDelegate next)
{
    private const string ApiKeyHeader = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context, IApplicationRepository appRepo)
    {
        // Skip for: Swagger UI/spec, auth pre-flight, SignalR hubs, health checks
        if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/api/auth") ||
            context.Request.Path.StartsWithSegments("/hubs") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var rawKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing X-Api-Key header.");
            return;
        }

        // TODO: SHA-256 hash rawKey, call appRepo.GetByApiKeyHashAsync, store app in HttpContext.Items
        await next(context);
    }
}
