using HuGuWeb.Api.Identity;
using HuGuWeb.RoomOperations.Infrastructure.Persistence;
using HuGuWeb.Workforce.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HuGuWeb.Api.Extensions;

public static class HealthCheckExtensions
{
    public static WebApplicationBuilder AddHuGuWebHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddDbContextCheck<AppIdentityDbContext>("identity-database", tags: ["ready"])
            .AddDbContextCheck<WorkforceDbContext>("workforce-database", tags: ["ready"])
            .AddDbContextCheck<RoomOperationsDbContext>("room-operations-database", tags: ["ready"]);

        return builder;
    }
}
