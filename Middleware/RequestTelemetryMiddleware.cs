using System.Diagnostics;

namespace SistemaDeControleComtac.Middleware;

public sealed class RequestTelemetryMiddleware(
    RequestDelegate next,
    ILogger<RequestTelemetryMiddleware> logger)
{
    public const string RequestIdHeader = "X-Request-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString("N");
        context.TraceIdentifier = requestId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[RequestIdHeader] = requestId;
            return Task.CompletedTask;
        });

        var activity = Activity.Current;
        activity?.SetTag("comtac.request.id", requestId);

        var startedAt = Stopwatch.GetTimestamp();
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["RequestId"] = requestId,
            ["TraceId"] = activity?.TraceId.ToString(),
            ["RequestMethod"] = context.Request.Method,
            ["RequestPath"] = context.Request.Path.Value
        });

        try
        {
            await next(context);

            logger.LogInformation(
                "HTTP request completed. {StatusCode} {DurationMs} {Authenticated}",
                context.Response.StatusCode,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                context.User.Identity?.IsAuthenticated == true);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled HTTP request error. {DurationMs} {Authenticated}",
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                context.User.Identity?.IsAuthenticated == true);
            throw;
        }
    }
}
