using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HuGuWeb.Workforce.Infrastructure.Persistence;

public sealed class WorkforceDbContextFactory : IDesignTimeDbContextFactory<WorkforceDbContext>
{
    public WorkforceDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets("huguweb-api-development")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("IdentityDatabase")
            ?? throw new InvalidOperationException("Connection string 'IdentityDatabase' is not configured.");

        var options = new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new WorkforceDbContext(options);
    }
}
