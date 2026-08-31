using HuGuWeb.Api.Authorization;
using HuGuWeb.Workforce.Infrastructure.Persistence;
using HuGuWeb.Workforce.Infrastructure.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
            var workforce = scope.ServiceProvider.GetRequiredService<WorkforceDbContext>();

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
                    workforce,
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
                await EnsurePersonaAsync(userManager, store, workforce, logger, persona, sharedPassword);
            }

            await CleanupOrphanEmployeeAccountLinksAsync(store, logger);
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
        WorkforceDbContext workforce,
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
        await EnsureMembershipAndRolesAsync(store, workforce, logger, existing.Id, persona);
        await EnsureEmployeeAccountLinkAsync(store, logger, existing.Id, persona);
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

    private static async Task EnsureMembershipAndRolesAsync(
        IAuthorizationStore store,
        WorkforceDbContext workforce,
        ILogger logger,
        string userId,
        DevelopmentPersonaDefinition persona)
    {
        var organizationId = DevelopmentWorkforceSeeder.OrganizationId;
        var propertyId = persona.PropertyId;
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
            membership = (await store.GetMembershipAsync(membership.Id, CancellationToken.None))!;
        }
        else if (!membership.IsActive)
        {
            membership.IsActive = true;
        }

        DeactivateObsoleteSeedMemberships(memberships, organizationId, propertyId, membership.Id);

        foreach (var roleCode in persona.AssignedRoleCodes)
        {
            var role = await store.FindRoleByCodeAsync(organizationId, roleCode, CancellationToken.None);
            if (role is null)
            {
                continue;
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
        }

        await EnsureDepartmentScopesAsync(store, workforce, logger, membership, persona);
        await store.SaveChangesAsync(CancellationToken.None);
    }

    private static async Task EnsureDepartmentScopesAsync(
        IAuthorizationStore store,
        WorkforceDbContext workforce,
        ILogger logger,
        UserMembership membership,
        DevelopmentPersonaDefinition persona)
    {
        if (persona.DepartmentScopeCodes is null || membership.PropertyId is null)
        {
            return;
        }

        var propertyId = membership.PropertyId.Value;
        var desired = new HashSet<Guid>();
        foreach (var code in persona.DepartmentScopeCodes)
        {
            var department = await workforce.Departments.FirstOrDefaultAsync(
                item => item.PropertyId == propertyId && item.Code == code && item.IsActive,
                CancellationToken.None);
            if (department is null)
            {
                logger.LogWarning(
                    "Development persona {Email} department scope {Code} was not found for property {PropertyId}.",
                    persona.Email,
                    code,
                    propertyId);
                continue;
            }

            desired.Add(department.Id);
        }

        var current = membership.DepartmentScopes.Select(item => item.DepartmentId).ToHashSet();
        if (current.SetEquals(desired))
        {
            return;
        }

        foreach (var extra in membership.DepartmentScopes.Where(item => !desired.Contains(item.DepartmentId)).ToArray())
        {
            store.RemoveDepartmentScope(extra);
        }

        foreach (var departmentId in desired.Where(id => !current.Contains(id)))
        {
            store.AddDepartmentScope(new UserMembershipDepartmentScope
            {
                Id = Guid.CreateVersion7(),
                UserMembershipId = membership.Id,
                DepartmentId = departmentId
            });
        }

        logger.LogInformation(
            "Development persona {Email} department scopes set to {Codes}.",
            persona.Email,
            string.Join(',', persona.DepartmentScopeCodes));
    }

    private static async Task EnsureEmployeeAccountLinkAsync(
        IAuthorizationStore store,
        ILogger logger,
        string userId,
        DevelopmentPersonaDefinition persona)
    {
        if (persona.LinkedEmployeeId is not Guid employeeId)
        {
            var unexpected = await store.FindLinkByUserAsync(userId, CancellationToken.None);
            if (unexpected is not null)
            {
                store.RemoveLink(unexpected);
                await store.SaveChangesAsync(CancellationToken.None);
                logger.LogInformation(
                    "Removed unexpected EmployeeAccountLink for non-employee persona {Email}.",
                    persona.Email);
            }

            return;
        }

        var linkId = persona.LinkedAccountLinkId ?? Guid.CreateVersion7();
        var byUser = await store.FindLinkByUserAsync(userId, CancellationToken.None);
        if (byUser is not null)
        {
            if (byUser.EmployeeId == employeeId && byUser.Id == linkId)
            {
                return;
            }

            store.RemoveLink(byUser);
            await store.SaveChangesAsync(CancellationToken.None);
            logger.LogInformation(
                "Corrected EmployeeAccountLink for development persona {Email}.",
                persona.Email);
        }

        var byEmployee = await store.FindLinkByEmployeeAsync(employeeId, CancellationToken.None);
        if (byEmployee is not null)
        {
            if (byEmployee.UserId == userId && byEmployee.Id == linkId)
            {
                return;
            }

            store.RemoveLink(byEmployee);
            await store.SaveChangesAsync(CancellationToken.None);
        }

        store.AddLink(new EmployeeAccountLink
        {
            Id = linkId,
            UserId = userId,
            EmployeeId = employeeId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await store.SaveChangesAsync(CancellationToken.None);
        logger.LogInformation(
            "Development persona {Email} linked to employee {EmployeeId}.",
            persona.Email,
            employeeId);
    }

    private static async Task CleanupOrphanEmployeeAccountLinksAsync(IAuthorizationStore store, ILogger logger)
    {
        var allowedEmployeeIds = DevelopmentPersonaCatalog.AdditionalPersonas
            .Where(item => item.LinkedEmployeeId.HasValue)
            .Select(item => item.LinkedEmployeeId!.Value)
            .ToHashSet();
        var links = await store.ListLinksAsync(CancellationToken.None);
        var removed = 0;
        foreach (var link in links)
        {
            if (allowedEmployeeIds.Contains(link.EmployeeId))
            {
                continue;
            }

            store.RemoveLink(link);
            removed++;
        }

        if (removed > 0)
        {
            await store.SaveChangesAsync(CancellationToken.None);
            logger.LogInformation(
                "Removed {Count} orphan development EmployeeAccountLink row(s).",
                removed);
        }
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
