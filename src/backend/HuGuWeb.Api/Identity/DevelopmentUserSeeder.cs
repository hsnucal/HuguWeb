using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using Microsoft.AspNetCore.Identity;

namespace HuGuWeb.Api.Identity;

public static class DevelopmentUserSeeder
{
    public static async Task TrySeedAsync(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        var logger = app.Logger;
        var configuration = app.Configuration;
        var broadEmail = configuration[DevelopmentPersonaCatalog.BroadEmailKey];
        var broadPassword = configuration[DevelopmentPersonaCatalog.BroadPasswordKey];
        var sharedPassword = configuration[DevelopmentPersonaCatalog.DefaultPasswordKey];
        if (string.IsNullOrWhiteSpace(sharedPassword))
        {
            sharedPassword = broadPassword;
        }

        try
        {
            using var scope = app.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            if (string.IsNullOrWhiteSpace(broadEmail) || string.IsNullOrWhiteSpace(broadPassword))
            {
                logger.LogInformation(
                    "Broad development user was not seeded. Set {EmailKey} and {PasswordKey} via user secrets or environment variables.",
                    DevelopmentPersonaCatalog.BroadEmailKey,
                    DevelopmentPersonaCatalog.BroadPasswordKey);
            }
            else
            {
                await EnsurePersonaAsync(
                    userManager,
                    logger,
                    DevelopmentPersonaCatalog.Broad(broadEmail),
                    broadPassword);
            }

            if (string.IsNullOrWhiteSpace(sharedPassword))
            {
                logger.LogInformation(
                    "Additional development personas were not seeded. Set {DefaultPasswordKey} or {PasswordKey} via user secrets or environment variables.",
                    DevelopmentPersonaCatalog.DefaultPasswordKey,
                    DevelopmentPersonaCatalog.BroadPasswordKey);
                return;
            }

            foreach (var persona in DevelopmentPersonaCatalog.AdditionalPersonas)
            {
                await EnsurePersonaAsync(userManager, logger, persona, sharedPassword);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Development users were not seeded because the identity database is unavailable.");
        }
    }

    private static async Task EnsurePersonaAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        DevelopmentPersonaDefinition persona,
        string password)
    {
        var existing = await userManager.FindByEmailAsync(persona.Email);
        if (existing is null)
        {
            var user = new ApplicationUser
            {
                UserName = persona.Email,
                Email = persona.Email,
                EmailConfirmed = true
            };

            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                logger.LogWarning(
                    "Development persona {Email} could not be created. Identity rejected the request without storing a password in logs.",
                    persona.Email);
                return;
            }

            existing = user;
            logger.LogInformation("Development persona {Email} was created.", persona.Email);
        }
        else
        {
            logger.LogInformation("Development persona {Email} already exists. Credentials were not changed.", persona.Email);
        }

        await ConvergePermissionsAsync(userManager, existing, persona.Permissions);
    }

    private static async Task ConvergePermissionsAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        IReadOnlyCollection<string> expectedPermissions)
    {
        var claims = await userManager.GetClaimsAsync(user);
        var current = claims
            .Where(claim => claim.Type == WorkforcePermissions.ClaimType)
            .Select(claim => claim.Value);
        var (add, remove) = DevelopmentPermissionConvergence.Diff(current, expectedPermissions);

        foreach (var permission in add)
        {
            await userManager.AddClaimAsync(user, new Claim(WorkforcePermissions.ClaimType, permission));
        }

        foreach (var permission in remove)
        {
            var claim = claims.First(item =>
                item.Type == WorkforcePermissions.ClaimType && item.Value == permission);
            await userManager.RemoveClaimAsync(user, claim);
        }
    }
}
