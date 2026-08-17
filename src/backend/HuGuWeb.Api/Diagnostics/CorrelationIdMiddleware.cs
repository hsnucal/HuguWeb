using System.Diagnostics;

namespace HuGuWeb.Api.Diagnostics;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var incoming = context.Request.Headers[CorrelationId.HeaderName].ToString();
        var fallback = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var correlationId = CorrelationId.Resolve(incoming, fallback);

        context.Items[CorrelationId.ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationId.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { [CorrelationId.ItemKey] = correlationId }))
        {
            await next(context);
        }
    }
}
