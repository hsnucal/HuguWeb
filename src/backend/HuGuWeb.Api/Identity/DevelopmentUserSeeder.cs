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
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                return;
            }

            var user = new IdentityUser
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

            logger.LogInformation("Development user {Email} was created.", email);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Development user was not seeded because the identity database is unavailable.");
        }
    }
}
