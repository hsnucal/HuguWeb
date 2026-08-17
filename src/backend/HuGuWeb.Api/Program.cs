using HuGuWeb.Api.Extensions;
using HuGuWeb.Api.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddHuGuWebObservability();
builder.AddHuGuWebPersistence();
builder.AddHuGuWebSecurity();
builder.AddHuGuWebHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHuGuWebPipeline();

if (app.Environment.IsDevelopment())
{
    await DevelopmentUserSeeder.TrySeedAsync(app);
}

app.Run();
