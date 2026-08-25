using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using HuGuWeb.Workforce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HuGuWeb.Workforce.Infrastructure;

public static class WorkforceServiceCollectionExtensions
{
    public static IServiceCollection AddWorkforceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDatabase")
            ?? throw new InvalidOperationException("Connection string 'IdentityDatabase' is not configured.");

        services.TryAddSingleton(TimeProvider.System);
        services.Configure<WorkplaceOptions>(configuration.GetSection(WorkplaceOptions.SectionName));
        services.Configure<EmployeePhotoStorageOptions>(
            configuration.GetSection(EmployeePhotoStorageOptions.SectionName));
        services.AddDbContext<WorkforceDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IWorkforceClock, SystemWorkforceClock>();
        services.AddSingleton<IWorkplaceContext, ConfiguredWorkplaceContext>();
        services.AddSingleton<IEmployeePhotoStorage, FileSystemEmployeePhotoStorage>();
        services.AddScoped<IWorkforceStore, EfWorkforceStore>();
        services.AddScoped<HireEmployeeUseCase>();
        services.AddScoped<HireEmployeeWithProfileUseCase>();
        services.AddScoped<UpdateEmployeeHrProfileUseCase>();
        services.AddScoped<EmployeePhotoUseCases>();
        services.AddScoped<TransferEmployeeUseCase>();
        services.AddScoped<EndEmploymentUseCase>();
        services.AddScoped<MaintainDepartmentsUseCase>();
        services.AddScoped<MaintainPositionsUseCase>();
        services.AddScoped<ActiveWorkforceQuery>();
        services.AddScoped<EmployeeHistoryQuery>();
        services.AddScoped<EmployeeDirectoryQuery>();
        services.AddScoped<HrEmployeeDirectoryQuery>();
        services.AddScoped<HrEmployeeCardQuery>();
        services.AddScoped<OfficialLookupsQuery>();
        services.AddScoped<MaintainSgkWorkplaceRegistrationsUseCase>();
        services.AddScoped<SaveOfficialEmploymentProfileUseCase>();
        return services;
    }
}
