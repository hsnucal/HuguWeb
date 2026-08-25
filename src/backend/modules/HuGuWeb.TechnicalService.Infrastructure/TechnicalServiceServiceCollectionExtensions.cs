using HuGuWeb.TechnicalService.Application;
using HuGuWeb.TechnicalService.Domain;
using HuGuWeb.TechnicalService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HuGuWeb.TechnicalService.Infrastructure;

public static class TechnicalServiceServiceCollectionExtensions
{
    public static IServiceCollection AddTechnicalServiceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDatabase")
            ?? throw new InvalidOperationException("Connection string 'IdentityDatabase' is not configured.");

        services.TryAddSingleton(TimeProvider.System);
        services.AddDbContext<TechnicalServiceDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<ITechnicalServiceClock, SystemTechnicalServiceClock>();
        services.AddScoped<ITechnicalServiceWorkplace, ConfiguredTechnicalServiceWorkplace>();
        services.AddScoped<ITechnicalServiceStore, EfTechnicalServiceStore>();
        services.AddScoped<IAssignableEmployeeDirectory, WorkforceAssignableEmployeeDirectory>();
        services.AddScoped<IRoomIdentityDirectory, RoomOperationsRoomDirectory>();
        services.AddScoped<IRoomPreparationImpactConsumer, RoomOperationsPreparationImpactConsumer>();
        services.AddScoped<HuGuWeb.RoomOperations.Application.IRoomServiceabilityLookup, TechnicalServiceRoomServiceabilityLookup>();
        services.AddScoped<CreateIssueUseCase>();
        services.AddScoped<AssignIssueUseCase>();
        services.AddScoped<ChangePriorityUseCase>();
        services.AddScoped<ChangeBlockingUseCase>();
        services.AddScoped<StartWorkUseCase>();
        services.AddScoped<MarkUnableToResolveUseCase>();
        services.AddScoped<ResumeWorkUseCase>();
        services.AddScoped<ResolveWorkUseCase>();
        services.AddScoped<ListIssuesQuery>();
        services.AddScoped<GetIssueDetailQuery>();
        services.AddScoped<ListAssignableEmployeesQuery>();
        services.AddScoped<ListRoomsQuery>();
        services.AddScoped<ListCategoriesQuery>();
        return services;
    }
}
