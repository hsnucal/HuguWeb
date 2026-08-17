using HuGuWeb.Api.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HuGuWeb.Api.Extensions;

public static class HealthCheckExtensions
{
    public static WebApplicationBuilder AddHuGuWebHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddDbContextCheck<AppIdentityDbContext>("identity-database", tags: ["ready"]);

        return builder;
    }
}
