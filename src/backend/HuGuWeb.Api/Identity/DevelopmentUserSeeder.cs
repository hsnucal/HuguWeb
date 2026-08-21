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
        var email = app.Configuration["DevelopmentUser:Email"];
        var password = app.Configuration["DevelopmentUser:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation(
                "Development user was not seeded. Set DevelopmentUser:Email and DevelopmentUser:Password via user secrets or environment variables.");
            return;
        }

        try
        {
            using var scope = app.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                await EnsureWorkforcePermissionsAsync(userManager, existing);
                return;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogWarning(
                    "Development user could not be created. Identity rejected the request without storing a password in logs.");
                return;
            }

            await EnsureWorkforcePermissionsAsync(userManager, user);
            logger.LogInformation("Development user {Email} was created.", email);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Development user was not seeded because the identity database is unavailable.");
        }
    }

    private static async Task EnsureWorkforcePermissionsAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user)
    {
        var claims = await userManager.GetClaimsAsync(user);
        await AddPermissionIfMissing(userManager, user, claims, WorkforcePermissions.Read);
        await AddPermissionIfMissing(userManager, user, claims, WorkforcePermissions.Manage);
    }

    private static async Task AddPermissionIfMissing(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        IList<Claim> claims,
        string permission)
    {
        if (claims.Any(claim =>
                claim.Type == WorkforcePermissions.ClaimType
                && claim.Value == permission))
        {
            return;
        }

        await userManager.AddClaimAsync(user, new Claim(WorkforcePermissions.ClaimType, permission));
    }
}
