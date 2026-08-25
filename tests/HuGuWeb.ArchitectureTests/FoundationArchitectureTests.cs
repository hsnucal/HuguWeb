using HuGuWeb.Api.Authorization;
using HuGuWeb.Workforce.Domain;
using HuGuWeb.Workforce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HuGuWeb.ArchitectureTests;

public class FoundationArchitectureTests
{
    [Fact]
    public void ProductionCode_HasNoPilotPropertyFallback()
    {
        var src = Path.Combine(FindRepoRoot(), "src");
        var files = Directory.GetFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                var name = Path.GetFileName(path);
                return !name.StartsWith("Development", StringComparison.Ordinal)
                    && !path.Contains($"{Path.DirectorySeparatorChar}Seeding{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
            })
            .ToArray();

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("pilot property", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("pilot Property", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("default hotel", text, StringComparison.OrdinalIgnoreCase);
        }

        var workplace = File.ReadAllText(Path.Combine(src, "backend", "HuGuWeb.Api", "Authorization", "RequestWorkplaceContext.cs"));
        Assert.DoesNotContain("WorkplaceOptions", workplace, StringComparison.Ordinal);
        var calculator = File.ReadAllText(Path.Combine(src, "backend", "HuGuWeb.Api", "Authorization", "EffectivePermissionCalculator.cs"));
        Assert.DoesNotContain("oldest", calculator, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Europe/Istanbul", workplace, StringComparison.Ordinal);
    }

    [Fact]
    public void Solution_HasNoBuildingBlocksDumpingGround()
    {
        var root = FindRepoRoot();
        Assert.Empty(Directory.GetDirectories(Path.Combine(root, "src"), "*BuildingBlocks*", SearchOption.AllDirectories));
        Assert.DoesNotContain(
            File.ReadAllText(Path.Combine(root, "HuGuWeb.slnx")),
            "BuildingBlocks",
            StringComparison.Ordinal);
    }

    [Fact]
    public void Property_OwnsTimeZoneId_InEfModel()
    {
        var options = new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseNpgsql("Host=localhost;Database=huguweb_model_check;Username=huguweb;Password=unused")
            .Options;

        using var context = new WorkforceDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Property));
        Assert.NotNull(entity);
        var timeZone = entity.FindProperty(nameof(Property.TimeZoneId));
        Assert.NotNull(timeZone);
        Assert.False(timeZone.IsNullable);
        Assert.Equal(Property.TimeZoneIdMaxLength, timeZone.GetMaxLength());
    }

    [Fact]
    public void AuthorizationRole_CodeIsUniqueWithinOrganization()
    {
        var options = new DbContextOptionsBuilder<HuGuWeb.Api.Identity.AppIdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=huguweb_model_check;Username=huguweb;Password=unused")
            .Options;

        using var context = new HuGuWeb.Api.Identity.AppIdentityDbContext(options);
        var entity = context.Model.FindEntityType(typeof(AuthorizationRole));
        Assert.NotNull(entity);
        var unique = entity.GetIndexes().Single(index =>
            index.IsUnique
            && index.GetDatabaseName() == "IX_AuthorizationRoles_OrganizationId_Code");
        Assert.Equal(["OrganizationId", "Code"], unique.Properties.Select(property => property.Name).ToArray());
    }

    [Fact]
    public void ActorContext_LivesInApiBoundary_NotDomain()
    {
        Assert.Equal("HuGuWeb.Api", typeof(HuGuWeb.Api.Context.ActorContext).Assembly.GetName().Name);
        Assert.Null(typeof(Employee).GetProperty("ActorContext"));
        Assert.Null(typeof(HuGuWeb.RoomOperations.Domain.Room).GetProperty("HttpContext"));
        Assert.DoesNotContain(
            GetReferencedAssemblyNames(typeof(Employee).Assembly),
            name => name.Equals("Microsoft.AspNetCore.Http.Abstractions", StringComparison.Ordinal));
    }

    [Fact]
    public void DomainAssemblies_DoNotUseFloatingPointMoney()
    {
        foreach (var type in new[]
                 {
                     typeof(Employee),
                     typeof(HuGuWeb.RoomOperations.Domain.Room),
                     typeof(HuGuWeb.TechnicalService.Domain.MaintenanceIssue)
                 })
        {
            var monetary = type.GetProperties()
                .Where(property => property.PropertyType == typeof(float) || property.PropertyType == typeof(double)
                    || property.PropertyType == typeof(float?) || property.PropertyType == typeof(double?))
                .Select(property => property.Name)
                .ToArray();
            Assert.Empty(monetary);
        }
    }

    [Fact]
    public void OperationalWorkplaceAdapters_AreScopedToRequestContext()
    {
        var src = Path.Combine(FindRepoRoot(), "src", "backend", "modules");
        var roomOps = File.ReadAllText(Path.Combine(
            src,
            "HuGuWeb.RoomOperations.Infrastructure",
            "RoomOperationsServiceCollectionExtensions.cs"));
        var technical = File.ReadAllText(Path.Combine(
            src,
            "HuGuWeb.TechnicalService.Infrastructure",
            "TechnicalServiceServiceCollectionExtensions.cs"));

        Assert.Contains("AddScoped<IRoomOperationsWorkplace, ConfiguredRoomOperationsWorkplace>", roomOps);
        Assert.DoesNotContain("AddSingleton<IRoomOperationsWorkplace", roomOps);
        Assert.Contains("AddScoped<ITechnicalServiceWorkplace, ConfiguredTechnicalServiceWorkplace>", technical);
        Assert.DoesNotContain("AddSingleton<ITechnicalServiceWorkplace", technical);
    }

    [Fact]
    public void Host_DoesNotReferenceRejectedDistributedInfrastructure()
    {
        var referenced = GetReferencedAssemblyNames(typeof(HuGuWeb.Api.Identity.AppIdentityDbContext).Assembly);
        Assert.DoesNotContain("MediatR", referenced);
        Assert.DoesNotContain("StackExchange.Redis", referenced);
        Assert.DoesNotContain("MassTransit", referenced);
        Assert.DoesNotContain("RabbitMQ.Client", referenced);
        var names = typeof(HuGuWeb.Api.Identity.AppIdentityDbContext).Assembly.GetTypes().Select(type => type.Name).ToArray();
        Assert.DoesNotContain("IRepository", names);
        Assert.DoesNotContain("IGenericRepository", names);
        Assert.DoesNotContain("GenericRepository", names);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "HuGuWeb.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate HuGuWeb.slnx from the test output directory.");
    }

    private static string[] GetReferencedAssemblyNames(System.Reflection.Assembly assembly) =>
        assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name ?? string.Empty)
            .ToArray();
}
