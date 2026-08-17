using HuGuWeb.Api.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HuGuWeb.Api.Extensions;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddHuGuWebObservability(this WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                if (context.HttpContext.Items[CorrelationId.ItemKey] is string correlationId)
                {
                    context.ProblemDetails.Extensions["correlationId"] = correlationId;
                }
            };
        });

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: "HuGuWeb.Api",
                serviceNamespace: "HuGuWeb"))
            .WithTracing(tracing => tracing.AddAspNetCoreInstrumentation())
            .WithMetrics(metrics => metrics.AddAspNetCoreInstrumentation());

        return builder;
    }
}
