using HuGuWeb.RoomOperations.Application;
using HuGuWeb.RoomOperations.Domain;
using HuGuWeb.RoomOperations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HuGuWeb.RoomOperations.Infrastructure;

public static class RoomOperationsServiceCollectionExtensions
{
    public static IServiceCollection AddRoomOperationsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("IdentityDatabase")
            ?? throw new InvalidOperationException("Connection string 'IdentityDatabase' is not configured.");

        services.AddDbContext<RoomOperationsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddSingleton<IRoomOperationsClock, SystemRoomOperationsClock>();
        services.AddSingleton<IRoomOperationsWorkplace, ConfiguredRoomOperationsWorkplace>();
        services.AddScoped<IRoomOperationsStore, EfRoomOperationsStore>();
        services.AddScoped<IAssignableEmployeeDirectory, WorkforceAssignableEmployeeDirectory>();
        services.AddScoped<RequestNeedsCleaningUseCase>();
        services.AddScoped<EnsurePreparationRequiredUseCase>();
        services.AddScoped<CompleteCleaningUseCase>();
        services.AddScoped<InspectRoomUseCase>();
        services.AddScoped<ListRoomOperationsQuery>();
        services.AddScoped<GetRoomOperationsDetailQuery>();
        services.AddScoped<ListAssignableEmployeesQuery>();
        return services;
    }
}
