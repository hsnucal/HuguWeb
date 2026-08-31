using System.Text.Json.Serialization;
using HuGuWeb.Api.Extensions;
using HuGuWeb.Api.Identity;
using HuGuWeb.RoomOperations.Infrastructure.Persistence;
using HuGuWeb.RoomOperations.Infrastructure.Seeding;
using HuGuWeb.TechnicalService.Infrastructure.Persistence;
using HuGuWeb.TechnicalService.Infrastructure.Seeding;
using HuGuWeb.Workforce.Infrastructure.Persistence;
using HuGuWeb.Workforce.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // CreateBuilder loads secrets via ApplicationName; coreclr can set that
    // to the DLL name and skip them. Bind to this assembly instead.
    builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);
}

builder.AddHuGuWebObservability();
builder.AddHuGuWebPersistence();
builder.AddHuGuWebSecurity();
builder.AddHuGuWebHealthChecks();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseHuGuWebPipeline();

if (app.Environment.IsDevelopment())
{
    // Workforce persona Employees must exist before EmployeeAccountLink seed.
    await TrySeedWorkforceAsync(app);
    await DevelopmentUserSeeder.TrySeedAsync(app);
    await TrySeedRoomOperationsAsync(app);
    await TrySeedTechnicalServiceAsync(app);
}

await TryEnsureDefaultLeaveTypesAsync(app);

app.Run();

static async Task TrySeedWorkforceAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WorkforceDbContext>();
        await DevelopmentWorkforceSeeder.TrySeedAsync(
            dbContext,
            app.Logger,
            cancellationToken: default,
            isDevelopment: app.Environment.IsDevelopment());
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Development workforce data was not seeded.");
    }
}

static async Task TrySeedRoomOperationsAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RoomOperationsDbContext>();
        await DevelopmentRoomOperationsSeeder.TrySeedAsync(dbContext, app.Logger);
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Development room operations data was not seeded.");
    }
}

static async Task TrySeedTechnicalServiceAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TechnicalServiceDbContext>();
        await DevelopmentTechnicalServiceSeeder.TrySeedAsync(dbContext, app.Logger);
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Development technical service data was not seeded.");
    }
}

static async Task TryEnsureDefaultLeaveTypesAsync(WebApplication app)
{
    try
    {
        using var scope = app.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<HuGuWeb.Workforce.Application.EnsureDefaultLeaveTypesUseCase>();
        var added = await useCase.ExecuteForAllOrganizationsAsync(CancellationToken.None);
        if (added > 0)
        {
            app.Logger.LogInformation("Default organization leave types were initialized ({Count} added).", added);
        }
    }
    catch (Exception exception)
    {
        app.Logger.LogWarning(exception, "Default organization leave types were not initialized.");
    }
}
