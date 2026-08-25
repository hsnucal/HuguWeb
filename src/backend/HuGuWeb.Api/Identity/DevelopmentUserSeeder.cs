using HuGuWeb.Api.Authorization;
using HuGuWeb.Workforce.Infrastructure.Seeding;
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
            var store = scope.ServiceProvider.GetRequiredService<IAuthorizationStore>();

            await EnsureSystemRolesAsync(store, logger);

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
                    store,
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
                await EnsurePersonaAsync(userManager, store, logger, persona, sharedPassword);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Development users were not seeded because the identity database is unavailable.");
        }
    }

    private static async Task EnsureSystemRolesAsync(IAuthorizationStore store, ILogger logger)
    {
        var organizationId = DevelopmentWorkforceSeeder.OrganizationId;
        var existing = await store.ListRolesAsync(organizationId, CancellationToken.None);
        foreach (var template in SystemRoleTemplates.All)
        {
            var role = existing.FirstOrDefault(item => item.Id == template.Id)
                ?? existing.FirstOrDefault(item => item.Code == template.Code);
            if (role is null)
            {
                role = new AuthorizationRole
                {
                    Id = template.Id,
                    OrganizationId = organizationId,
                    Name = template.Name,
                    Code = template.Code,
                    ScopeType = template.ScopeType,
                    IsSystemTemplate = true,
                    IsActive = true
                };
                store.AddRole(role);
                logger.LogInformation("Development role {RoleCode} was created.", template.Code);
            }
            else
            {
                role.Name = template.Name;
                role.Code = template.Code;
                role.ScopeType = template.ScopeType;
                role.IsSystemTemplate = true;
                role.IsActive = true;
            }

            var current = role.Permissions.Select(item => item.PermissionCode).ToHashSet(StringComparer.Ordinal);
            foreach (var permission in template.Permissions.Where(code => !current.Contains(code)))
            {
                store.AddPermission(new RolePermission { RoleId = role.Id, PermissionCode = permission });
            }

            foreach (var extra in role.Permissions.Where(item => !template.Permissions.Contains(item.PermissionCode, StringComparer.Ordinal)).ToArray())
            {
                store.RemovePermission(extra);
            }
        }

        await store.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task EnsurePersonaAsync(
        UserManager<ApplicationUser> userManager,
        IAuthorizationStore store,
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

        await RemoveLegacyPermissionClaimsAsync(userManager, existing);
        await EnsureMembershipAndRoleAsync(store, existing.Id, persona.RoleCode, persona.PropertyId);
        await userManager.UpdateSecurityStampAsync(existing);
    }

    private static async Task RemoveLegacyPermissionClaimsAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user)
    {
        var claims = await userManager.GetClaimsAsync(user);
        foreach (var claim in claims.Where(item => item.Type == WorkforcePermissions.ClaimType))
        {
            await userManager.RemoveClaimAsync(user, claim);
        }
    }

    private static async Task EnsureMembershipAndRoleAsync(
        IAuthorizationStore store,
        string userId,
        string roleCode,
        Guid? propertyId)
    {
        var organizationId = DevelopmentWorkforceSeeder.OrganizationId;
        var memberships = await store.ListMembershipsForUserAsync(userId, CancellationToken.None);
        var membership = memberships.FirstOrDefault(item =>
            item.OrganizationId == organizationId && item.PropertyId == propertyId);
        if (membership is null)
        {
            membership = new UserMembership
            {
                Id = Guid.CreateVersion7(),
                UserId = userId,
                OrganizationId = organizationId,
                PropertyId = propertyId,
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            store.AddMembership(membership);
            await store.SaveChangesAsync(CancellationToken.None);
        }
        else if (!membership.IsActive)
        {
            membership.IsActive = true;
        }

        DeactivateObsoleteSeedMemberships(memberships, organizationId, propertyId, membership.Id);

        var role = await store.FindRoleByCodeAsync(organizationId, roleCode, CancellationToken.None);
        if (role is null)
        {
            return;
        }

        if (membership.RoleAssignments.All(item => item.RoleId != role.Id))
        {
            store.AddAssignment(new UserRoleAssignment
            {
                Id = Guid.CreateVersion7(),
                MembershipId = membership.Id,
                RoleId = role.Id
            });
        }

        await store.SaveChangesAsync(CancellationToken.None);
    }

    /// <summary>
    /// Development seed only. Earlier AUTH-01 seeds gave org-wide personas a Property membership.
    /// Deactivate those leftover rows so manual tests match the intended catalog scope.
    /// Runtime authorization never reads persona emails.
    /// </summary>
    private static void DeactivateObsoleteSeedMemberships(
        IReadOnlyList<UserMembership> memberships,
        Guid organizationId,
        Guid? intendedPropertyId,
        Guid intendedMembershipId)
    {
        foreach (var extra in memberships.Where(item =>
            item.Id != intendedMembershipId
            && item.OrganizationId == organizationId
            && item.PropertyId != intendedPropertyId
            && item.IsActive))
        {
            extra.IsActive = false;
        }
    }
}
