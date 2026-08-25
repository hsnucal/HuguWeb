using System.Text.Json;
using HuGuWeb.Api.Diagnostics;
using HuGuWeb.Api.Identity;
using HuGuWeb.Api.Localization;
using Microsoft.AspNetCore.Localization;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace HuGuWeb.Api.Extensions;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddHuGuWebObservability(this WebApplicationBuilder builder)
    {
        builder.Services.AddLocalization();
        builder.Services.AddScoped<ApiErrorLocalizer>();
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            var cultures = SupportedLanguages.All.ToArray();
            options.SetDefaultCulture(SupportedLanguages.Default);
            options.AddSupportedCultures(cultures);
            options.AddSupportedUICultures(cultures);
            options.RequestCultureProviders =
            [
                new AcceptLanguageHeaderRequestCultureProvider()
            ];
        });

        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                if (context.HttpContext.Items[CorrelationId.ItemKey] is string correlationId)
                {
                    context.ProblemDetails.Extensions["correlationId"] = correlationId;
                }

                if (!TryReadCode(context.ProblemDetails.Extensions, out var code))
                {
                    return;
                }

                var localizer = context.HttpContext.RequestServices.GetService<ApiErrorLocalizer>();
                if (localizer is null)
                {
                    return;
                }

                var title = localizer[$"error.{code}.title"];
                if (!title.ResourceNotFound)
                {
                    context.ProblemDetails.Title = title.Value;
                }

                var detail = localizer[$"error.{code}.detail"];
                if (!detail.ResourceNotFound)
                {
                    context.ProblemDetails.Detail = detail.Value;
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

    private static bool TryReadCode(IDictionary<string, object?> extensions, out string code)
    {
        code = string.Empty;
        if (!extensions.TryGetValue("code", out var raw) || raw is null)
        {
            return false;
        }

        code = raw switch
        {
            string value => value,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString() ?? string.Empty,
            _ => raw.ToString() ?? string.Empty
        };

        return !string.IsNullOrWhiteSpace(code);
    }
}
