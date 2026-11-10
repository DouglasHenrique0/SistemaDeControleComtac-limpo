using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SistemaDeControleComtac.Health;

public static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.Headers.CacheControl = "no-store";

        var response = new
        {
            status = report.Status.ToString(),
            durationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    durationMs = entry.Value.Duration.TotalMilliseconds
                })
        };

        return context.Response.WriteAsJsonAsync(response);
    }
}
