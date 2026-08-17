using HuGuWeb.Api.Identity;

namespace HuGuWeb.ArchitectureTests;

public class ProductionAssemblyGuardTests
{
    [Fact]
    public void Api_DoesNotReference_TestAssemblies()
    {
        var referencedNames = GetReferencedAssemblyNames();

        Assert.DoesNotContain(referencedNames, name =>
            name.Contains("Test", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Api_DoesNotReference_RejectedBootstrapDependencies()
    {
        var referencedNames = GetReferencedAssemblyNames();

        string[] forbidden =
        [
            "MediatR",
            "StackExchange.Redis",
            "MassTransit",
            "RabbitMQ.Client",
            "Hangfire.Core",
            "Hangfire.AspNetCore",
            "Microsoft.EntityFrameworkCore.Sqlite",
            "MongoDB.Driver"
        ];

        foreach (var name in forbidden)
        {
            Assert.DoesNotContain(name, referencedNames);
        }
    }

    [Fact]
    public void IdentityContext_DoesNotExpose_HotelDomainSets()
    {
        var dbSetProperties = typeof(AppIdentityDbContext)
            .GetProperties()
            .Where(property => property.PropertyType.IsGenericType
                && property.PropertyType.Name.StartsWith("DbSet", StringComparison.Ordinal))
            .Select(property => property.Name)
            .ToArray();

        string[] forbidden =
        [
            "Hotels",
            "Properties",
            "Tenants",
            "Rooms",
            "Reservations",
            "Guests",
            "Housekeeping",
            "Folios"
        ];

        foreach (var name in forbidden)
        {
            Assert.DoesNotContain(name, dbSetProperties);
        }
    }

    private static string[] GetReferencedAssemblyNames() =>
        typeof(AppIdentityDbContext).Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name ?? string.Empty)
            .ToArray();
}
