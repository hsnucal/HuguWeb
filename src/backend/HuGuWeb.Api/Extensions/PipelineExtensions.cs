using HuGuWeb.Api.Diagnostics;
using HuGuWeb.Api.Endpoints;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace HuGuWeb.Api.Extensions;

public static class PipelineExtensions
{
    public static WebApplication UseHuGuWebPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseMiddleware<CorrelationIdMiddleware>();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseCors(SecurityExtensions.CorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        app.MapAuthEndpoints();
        app.MapWorkforceEndpoints();
        app.MapRoomOperationsEndpoints();

        return app;
    }
}
