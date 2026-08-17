using HuGuWeb.Api.Identity;
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

        return builder;
    }
}
