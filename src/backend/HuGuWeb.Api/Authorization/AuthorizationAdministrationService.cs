using HuGuWeb.Api.Identity;
using HuGuWeb.Workforce.Application;
using Microsoft.AspNetCore.Identity;

namespace HuGuWeb.Api.Authorization;

public sealed class AuthorizationAdministrationService(
    IAuthorizationStore store,
    IWorkforceStore workforce,
    UserManager<ApplicationUser> userManager,
    SecurityStampRefreshService stampRefresh,
    LastAdministratorProtectionService lastAdministrator,
    TimeProvider time)
{
    public async Task<AccessSnapshot> GetAccessSnapshotAsync(
        string userId,
        Guid? selectedPropertyId,
        CancellationToken cancellationToken) =>
        await new AccessSnapshotService(store).GetSnapshotAsync(userId, selectedPropertyId, cancellationToken);

    public async Task<AuthorizationResult<ApplicationUser>> CreateUserAsync(
        string email,
        string password,
        Guid? employeeId,
        string? actorUserId,
        Guid? actorOrganizationId,
        Guid? actorPropertyId,
        CancellationToken cancellationToken)
    {
        var normalized = email.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || string.IsNullOrWhiteSpace(password))
        {
            return AuthorizationError.InvalidRequest("invalid-request", "Email and password are required.");
        }

        if (await userManager.FindByEmailAsync(normalized) is not null)
        {
            return AuthorizationError.EmailInUse();
        }

        if (employeeId is Guid linkedEmployee)
        {
            var existingLink = await store.FindLinkByEmployeeAsync(linkedEmployee, cancellationToken);
            if (existingLink is not null)
            {
                return AuthorizationError.EmployeeAlreadyLinked();
            }
        }

        var user = new ApplicationUser
        {
            UserName = normalized,
            Email = normalized,
            EmailConfirmed = true
        };
        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return AuthorizationError.PasswordRejected();
        }

        if (employeeId is Guid employee)
        {
            store.AddLink(new EmployeeAccountLink
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                EmployeeId = employee,
                CreatedAtUtc = time.GetUtcNow()
            });
            AddAudit(
                actorUserId,
                actorOrganizationId,
                actorPropertyId,
                AuthorizationAuditActions.EmployeeLinked,
                user.Id,
                details: employee.ToString());
        }

        AddAudit(
            actorUserId,
            actorOrganizationId,
            actorPropertyId,
            AuthorizationAuditActions.UserCreated,
            user.Id,
            details: normalized);
        await store.SaveChangesAsync(cancellationToken);
        return AuthorizationResult<ApplicationUser>.Success(user);
    }

    public async Task<AuthorizationResult<UserMembership>> CreateMembershipAsync(
        string userId,
        Guid organizationId,
        Guid? propertyId,
        string? actorUserId,
        Guid? actorOrganizationId,
        Guid? actorPropertyId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return AuthorizationError.UserNotFound();
        }

        var organization = await workforce.GetOrganizationAsync(organizationId, cancellationToken);
        if (organization is null)
        {
            return AuthorizationError.InvalidRequest("organization-not-found", "The organization was not found.");
        }

        if (propertyId is Guid property)
        {
            var entity = await workforce.GetPropertyAsync(property, cancellationToken);
            if (entity is null || entity.OrganizationId != organizationId)
            {
                return AuthorizationError.PropertyNotInOrganization();
            }
        }

        var existing = await store.ListMembershipsForUserAsync(userId, cancellationToken);
        var duplicate = existing.Any(item =>
            item.OrganizationId == organizationId
            && item.PropertyId == propertyId);
        if (duplicate)
        {
            return AuthorizationError.DuplicateMembership();
        }

        var membership = new UserMembership
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            OrganizationId = organizationId,
            PropertyId = propertyId,
            IsActive = true,
            CreatedAtUtc = time.GetUtcNow()
        };
        store.AddMembership(membership);
        AddAudit(
            actorUserId,
            actorOrganizationId,
            actorPropertyId,
            AuthorizationAuditActions.MembershipCreated,
            userId,
            membership.Id);
        await store.SaveChangesAsync(cancellationToken);
        await stampRefresh.RefreshAsync(userId);
        return AuthorizationResult<UserMembership>.Success(membership);
    }

    public async Task<AuthorizationResult> SetMembershipActiveAsync(
        Guid membershipId,
        bool isActive,
        string? actorUserId,
        Guid? actorOrganizationId,
        Guid? actorPropertyId,
        CancellationToken cancellationToken)
    {
        var membership = await store.GetMembershipAsync(membershipId, cancellationToken);
        if (membership is null)
        {
            return AuthorizationError.MembershipNotFound();
        }

        if (!isActive)
        {
            var retains = await lastAdministrator.WouldRetainAdministrationAsync(
                membership.OrganizationId,
                membership.Id,
                null,
                null,
                null,
                null,
                cancellationToken);
            if (!retains)
            {
                return AuthorizationError.LastAdministrator();
            }
        }

        membership.IsActive = isActive;
        AddAudit(
            actorUserId,
            actorOrganizationId,
            actorPropertyId,
            isActive ? AuthorizationAuditActions.MembershipActivated : AuthorizationAuditActions.MembershipDeactivated,
            membership.UserId,
            membership.Id);
        await store.SaveChangesAsync(cancellationToken);
        await stampRefresh.RefreshAsync(membership.UserId);
        return AuthorizationResult.Success();
    }

    public async Task<AuthorizationResult<AuthorizationRole>> CreateRoleAsync(
        Guid organizationId,
        string name,
        string code,
        AuthorizationScopeType scopeType,
        string? actorUserId,
        Guid? actorOrganizationId,
        Guid? actorPropertyId,
        CancellationToken cancellationToken)
    {
        var trimmedCode = code.Trim().ToLowerInvariant();
        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedCode) || string.IsNullOrWhiteSpace(trimmedName))
        {
            return AuthorizationError.InvalidRequest("invalid-request", "Role name and code are required.");
        }

        var organization = await workforce.GetOrganizationAsync(organizationId, cancellationToken);
        if (organization is null)
        {
            return AuthorizationError.InvalidRequest("organization-not-found", "The organization was not found.");
        }

        if (await store.FindRoleByCodeAsync(organizationId, trimmedCode, cancellationToken) is not null)
        {
            return AuthorizationError.DuplicateRoleCode();
        }

        var role = new AuthorizationRole
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            Name = trimmedName,
            Code = trimmedCode,
            ScopeType = scopeType,
            IsSystemTemplate = false,
            IsActive = true
        };
        store.AddRole(role);
        AddAudit(
            actorUserId,
            actorOrganizationId,
            actorPropertyId,
            AuthorizationAuditActions.RolePermissionChanged,
            roleId: role.Id,
            details: "created");
        await store.SaveChangesAsync(cancellationToken);
        return AuthorizationResult<AuthorizationRole>.Success(role);
    }

    public async Task<AuthorizationResult> SetRoleActiveAsync(
        Guid roleId,
        bool isActive,
        string? actorUserId,
        Guid? actorOrganizationId,
        Guid? actorPropertyId,
        CancellationToken cancellationToken)
    {
        var role = await store.GetRoleAsync(roleId, cancellationToken);
        if (role is null)
        {
            return AuthorizationError.RoleNotFound();
        }

        if (!isActive)
        {
            var retains = await lastAdministrator.WouldRetainAdministrationAsync(
                role.OrganizationId,
                null,
                role.Id,
                null,
                null,
                null,
                cancellationToken);
            if (!retains)
            {
                return AuthorizationError.LastAdministrator();
            }
        }

        role.IsActive = isActive;
        AddAudit(
            actorUserId,
            actorOrganizationId,
            actorPropertyId,
            AuthorizationAuditActions.RolePermissionChanged,
            roleId: role.Id,
            details: isActive ? "activated" : "deactivated");
        await store.SaveChangesAsync(cancellationToken);
        await stampRefresh.RefreshManyAsync(await store.ListUserIdsForRoleAsync(roleId, cancellationToken));
        return AuthorizationResult.Success();
    }

    public async Task<AuthorizationResult> ReplaceRolePermissionsAsync(
        Guid roleId,
        IReadOnlyCollection<string> permissionCodes,
        string? actorUserId,
        Guid? actorOrganizationId,
        Guid? actorPropertyId,
        CancellationToken cancellationToken)
    {
        var role = await store.GetRoleAsync(roleId, cancellationToken);
        if (role is null)
        {
            return AuthorizationError.RoleNotFound();
        }

        var distinct = permissionCodes.Distinct(StringComparer.Ordinal).ToArray();
        if (distinct.Any(code => !PermissionCatalog.IsKnown(code)))
        {
            return AuthorizationError.InvalidPermissionCode();
        }

        var retains = await lastAdministrator.WouldRetainAdministrationAsync(
            role.OrganizationId,
            null,
            null,
            new Dictionary<Guid, IReadOnlyList<string>> { [role.Id] = distinct },
            null,
            null,
            cancellationToken);
        if (!retains)
        {
            return AuthorizationError.LastAdministrator();
        }

        foreach (var existing in role.Permissions.ToArray())
        {
            store.RemovePermission(existing);
        }

        foreach (var code in distinct)
        {
            store.AddPermission(new RolePermission { RoleId = role.Id, PermissionCode = code });
        }

        AddAudit(
            actorUserId,
            actorOrganizationId,
            actorPropertyId,
            AuthorizationAuditActions.RolePermissionChanged,
            roleId: role.Id,
            details: string.Join(',', distinct));
        await store.SaveChangesAsync(cancellationToken);
        await stampRefresh.RefreshManyAsync(await store.ListUserIdsForRoleAsync(roleId, cancellationToken));
        return AuthorizationResult.Success();
    }

    public async Task<AuthorizationResult> AssignRoleAsync(
        Guid membershipId,
        Guid roleId,
        string? actorUserId,
        Guid? actorOrganizationId,
        Guid? actorPropertyId,
        CancellationToken cancellationToken)
    {
        var membership = await store.GetMembershipAsync(membershipId, cancellationToken);
        if (membership is null)
        {
            return AuthorizationError.MembershipNotFound();
        }

        var role = await store.GetRoleAsync(roleId, cancellationToken);
        if (role is null)
        {
            return AuthorizationError.RoleNotFound();
        }

        if (!role.IsActive)
        {
            return AuthorizationError.RoleInactive();
        }

        if (role.OrganizationId != membership.OrganizationId || role.ScopeType != membership.ScopeType)
        {
            return AuthorizationError.ScopeMismatch();
        }

        if (membership.RoleAssignments.Any(item => item.RoleId == roleId))
        {
            return AuthorizationResult.Success();
        }

        store.AddAssignment(new UserRoleAssignment
        {
            Id = Guid.CreateVersion7(),
            MembershipId = membershipId,
            RoleId = roleId
        });
        AddAudit(
            actorUserId,
            actorOrganizationId,
            actorPropertyId,
            AuthorizationAuditActions.RoleAssigned,
            membership.UserId,
            membership.Id,
            role.Id);
        await store.SaveChangesAsync(cancellationToken);
        await stampRefresh.RefreshAsync(membership.UserId);
        return AuthorizationResult.Success();
    }

    public async Task<AuthorizationResult> RemoveRoleAsync(
        Guid membershipId,
        Guid roleId,
        string? actorUserId,
        Guid? actorOrganizationId,
        Guid? actorPropertyId,
        CancellationToken cancellationToken)
    {
        var membership = await store.GetMembershipAsync(membershipId, cancellationToken);
        if (membership is null)
        {
            return AuthorizationError.MembershipNotFound();
        }

        var assignment = membership.RoleAssignments.FirstOrDefault(item => item.RoleId == roleId);
        if (assignment is null)
        {
            return AuthorizationResult.Success();
        }

        var retains = await lastAdministrator.WouldRetainAdministrationAsync(
            membership.OrganizationId,
            null,
            null,
            null,
            membership.Id,
            roleId,
            cancellationToken);
        if (membership.IsActive && !retains)
        {
            return AuthorizationError.LastAdministrator();
        }

        store.RemoveAssignment(assignment);
        AddAudit(
            actorUserId,
            actorOrganizationId,
            actorPropertyId,
            AuthorizationAuditActions.RoleRemoved,
            membership.UserId,
            membership.Id,
            roleId);
        await store.SaveChangesAsync(cancellationToken);
        await stampRefresh.RefreshAsync(membership.UserId);
        return AuthorizationResult.Success();
    }

    private void AddAudit(
        string? actorUserId,
        Guid? actorOrganizationId,
        Guid? actorPropertyId,
        string action,
        string? subjectUserId = null,
        Guid? membershipId = null,
        Guid? roleId = null,
        string? details = null)
    {
        store.AddAudit(new AuthorizationAuditRecord
        {
            Id = Guid.CreateVersion7(),
            OccurredAtUtc = time.GetUtcNow(),
            ActorUserId = actorUserId,
            ActorOrganizationId = actorOrganizationId,
            ActorPropertyId = actorPropertyId,
            Action = action,
            SubjectUserId = subjectUserId,
            MembershipId = membershipId,
            RoleId = roleId,
            Details = details
        });
    }
}
