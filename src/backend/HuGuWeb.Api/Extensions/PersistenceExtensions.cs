using HuGuWeb.Api.Identity;
using HuGuWeb.RoomOperations.Infrastructure;
using HuGuWeb.Workforce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HuGuWeb.Api.Extensions;

public static class PersistenceExtensions
{
    public static WebApplicationBuilder AddHuGuWebPersistence(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("IdentityDatabase")
            ?? throw new InvalidOperationException("Connection string 'IdentityDatabase' is not configured.");

        builder.Services.AddDbContext<AppIdentityDbContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddWorkforceModule(builder.Configuration);
        builder.Services.AddRoomOperationsModule(builder.Configuration);

        return builder;
    }
}
