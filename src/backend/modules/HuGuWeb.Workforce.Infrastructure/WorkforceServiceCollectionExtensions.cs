using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using HuGuWeb.Workforce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HuGuWeb.Workforce.Infrastructure;

public static class WorkforceServiceCollectionExtensions
{
    public static IServiceCollection AddWorkforceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDatabase")
            ?? throw new InvalidOperationException("Connection string 'IdentityDatabase' is not configured.");

        services.Configure<WorkplaceOptions>(configuration.GetSection(WorkplaceOptions.SectionName));
        services.AddDbContext<WorkforceDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IWorkforceClock, SystemWorkforceClock>();
        services.AddSingleton<IWorkplaceContext, ConfiguredWorkplaceContext>();
        services.AddScoped<IWorkforceStore, EfWorkforceStore>();
        services.AddScoped<HireEmployeeUseCase>();
        services.AddScoped<TransferEmployeeUseCase>();
        services.AddScoped<EndEmploymentUseCase>();
        services.AddScoped<MaintainDepartmentsUseCase>();
        services.AddScoped<MaintainPositionsUseCase>();
        services.AddScoped<ActiveWorkforceQuery>();
        services.AddScoped<EmployeeHistoryQuery>();
        services.AddScoped<EmployeeDirectoryQuery>();
        return services;
    }
}
