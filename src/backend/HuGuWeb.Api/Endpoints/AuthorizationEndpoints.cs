using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Identity;
using HuGuWeb.Workforce.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HuGuWeb.Api.Endpoints;

public static class AuthorizationEndpoints
{
    public static IEndpointRouteBuilder MapAuthorizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var users = endpoints.MapGroup("/api/authorization/users")
            .WithTags("Authorization Users")
            .RequireAuthorization(AuthorizationPolicies.AuthorizationUsersManage);

        users.MapGet("/", ListUsers).WithName("ListAuthorizationUsers");
        users.MapGet("/{id}", GetUser).WithName("GetAuthorizationUser");
        users.MapPost("/", CreateUser)
            .WithName("CreateAuthorizationUser")
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        users.MapPost("/{id}/memberships", CreateMembership)
            .WithName("CreateAuthorizationMembership")
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        users.MapPatch("/memberships/{membershipId:guid}", SetMembershipActive)
            .WithName("SetAuthorizationMembershipActive")
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        users.MapPost("/memberships/{membershipId:guid}/roles/{roleId:guid}", AssignRole)
            .WithName("AssignAuthorizationRole")
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        users.MapDelete("/memberships/{membershipId:guid}/roles/{roleId:guid}", RemoveRole)
            .WithName("RemoveAuthorizationRole")
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        users.MapPut("/memberships/{membershipId:guid}/department-scopes", ReplaceDepartmentScopes)
            .WithName("ReplaceAuthorizationMembershipDepartmentScopes")
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        var roles = endpoints.MapGroup("/api/authorization/roles")
            .WithTags("Authorization Roles")
            .RequireAuthorization(AuthorizationPolicies.Authenticated);

        roles.MapGet("/", ListRoles)
            .WithName("ListAuthorizationRoles")
            .RequireAuthorization(policy => policy.RequireAssertion(HasUserOrRoleAdministration));
        roles.MapGet("/permissions", ListPermissionCatalog)
            .WithName("ListAuthorizationPermissions")
            .RequireAuthorization(policy => policy.RequireAssertion(HasUserOrRoleAdministration));
        roles.MapPost("/", CreateRole)
            .WithName("CreateAuthorizationRole")
            .RequireAuthorization(AuthorizationPolicies.AuthorizationRolesManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        roles.MapPatch("/{id:guid}", SetRoleActive)
            .WithName("SetAuthorizationRoleActive")
            .RequireAuthorization(AuthorizationPolicies.AuthorizationRolesManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        roles.MapPut("/{id:guid}/permissions", ReplaceRolePermissions)
            .WithName("ReplaceAuthorizationRolePermissions")
            .RequireAuthorization(AuthorizationPolicies.AuthorizationRolesManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static bool HasUserOrRoleAdministration(AuthorizationHandlerContext context) =>
        context.User.HasClaim(AuthorizationPermissions.ClaimType, AuthorizationPermissions.UsersManage)
        || context.User.HasClaim(AuthorizationPermissions.ClaimType, AuthorizationPermissions.RolesManage);

    private static async Task<IResult> ListUsers(
        ClaimsPrincipal actor,
        UserManager<ApplicationUser> userManager,
        IAuthorizationStore store,
        IWorkforceStore workforce,
        AccessSnapshotService snapshots,
        CancellationToken cancellationToken)
    {
        var users = userManager.Users.OrderBy(item => item.Email).ToArray();
        var memberships = await store.ListMembershipsAsync(cancellationToken);
        var links = await store.ListLinksAsync(cancellationToken);
        var selectedProperty = TryGuid(actor, AuthorizationClaims.PropertyId);
        var items = new List<AuthorizationUserListItem>();
        foreach (var user in users)
        {
            var snapshot = await snapshots.GetSnapshotAsync(user.Id, selectedProperty, cancellationToken);
            var userMemberships = memberships.Where(item => item.UserId == user.Id).ToArray();
            var link = links.FirstOrDefault(item => item.UserId == user.Id);
            items.Add(new AuthorizationUserListItem(
                user.Id,
                user.Email,
                user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow,
                link?.EmployeeId,
                await ToMembershipsAsync(userMemberships, workforce, cancellationToken),
                snapshot.Permissions));
        }

        return Results.Ok(items);
    }

    private static async Task<IResult> GetUser(
        string id,
        ClaimsPrincipal actor,
        UserManager<ApplicationUser> userManager,
        IAuthorizationStore store,
        IWorkforceStore workforce,
        AccessSnapshotService snapshots,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return AuthorizationError.UserNotFound().ToHttp();
        }

        var snapshot = await snapshots.GetSnapshotAsync(
            id,
            TryGuid(actor, AuthorizationClaims.PropertyId),
            cancellationToken);
        var memberships = await store.ListMembershipsForUserAsync(id, cancellationToken);
        var link = await store.FindLinkByUserAsync(id, cancellationToken);
        return Results.Ok(new AuthorizationUserListItem(
            user.Id,
            user.Email,
            user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow,
            link?.EmployeeId,
            await ToMembershipsAsync(memberships, workforce, cancellationToken),
            snapshot.Permissions));
    }

    private static async Task<IResult> CreateUser(
        ClaimsPrincipal actor,
        [FromBody] CreateAuthorizationUserRequest request,
        AuthorizationAdministrationService administration,
        CancellationToken cancellationToken)
    {
        var result = await administration.CreateUserAsync(
            request.Email,
            request.Password,
            request.EmployeeId,
            ActorUserId(actor),
            TryGuid(actor, AuthorizationClaims.OrganizationId),
            TryGuid(actor, AuthorizationClaims.PropertyId),
            cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/authorization/users/{result.Value!.Id}", new { result.Value.Id, result.Value.Email })
            : result.Error!.ToHttp();
    }

    private static async Task<IResult> CreateMembership(
        string id,
        ClaimsPrincipal actor,
        [FromBody] CreateMembershipRequest request,
        AuthorizationAdministrationService administration,
        CancellationToken cancellationToken)
    {
        var result = await administration.CreateMembershipAsync(
            id,
            request.OrganizationId,
            request.PropertyId,
            ActorUserId(actor),
            TryGuid(actor, AuthorizationClaims.OrganizationId),
            TryGuid(actor, AuthorizationClaims.PropertyId),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToMembership(result.Value!)) : result.Error!.ToHttp();
    }

    private static async Task<IResult> SetMembershipActive(
        Guid membershipId,
        ClaimsPrincipal actor,
        [FromBody] SetActiveRequest request,
        AuthorizationAdministrationService administration,
        CancellationToken cancellationToken)
    {
        var result = await administration.SetMembershipActiveAsync(
            membershipId,
            request.IsActive,
            ActorUserId(actor),
            TryGuid(actor, AuthorizationClaims.OrganizationId),
            TryGuid(actor, AuthorizationClaims.PropertyId),
            cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToHttp();
    }

    private static async Task<IResult> AssignRole(
        Guid membershipId,
        Guid roleId,
        ClaimsPrincipal actor,
        AuthorizationAdministrationService administration,
        CancellationToken cancellationToken)
    {
        var result = await administration.AssignRoleAsync(
            membershipId,
            roleId,
            ActorUserId(actor),
            TryGuid(actor, AuthorizationClaims.OrganizationId),
            TryGuid(actor, AuthorizationClaims.PropertyId),
            cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToHttp();
    }

    private static async Task<IResult> RemoveRole(
        Guid membershipId,
        Guid roleId,
        ClaimsPrincipal actor,
        AuthorizationAdministrationService administration,
        CancellationToken cancellationToken)
    {
        var result = await administration.RemoveRoleAsync(
            membershipId,
            roleId,
            ActorUserId(actor),
            TryGuid(actor, AuthorizationClaims.OrganizationId),
            TryGuid(actor, AuthorizationClaims.PropertyId),
            cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToHttp();
    }

    private static async Task<IResult> ReplaceDepartmentScopes(
        Guid membershipId,
        ClaimsPrincipal actor,
        [FromBody] ReplaceDepartmentScopesRequest request,
        AuthorizationAdministrationService administration,
        IWorkforceStore workforce,
        CancellationToken cancellationToken)
    {
        var result = await administration.ReplaceDepartmentScopesAsync(
            membershipId,
            request.DepartmentIds ?? [],
            ActorUserId(actor),
            TryGuid(actor, AuthorizationClaims.OrganizationId),
            TryGuid(actor, AuthorizationClaims.PropertyId),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!.ToHttp();
        }

        return Results.Ok(await ToMembershipAsync(result.Value!, workforce, cancellationToken));
    }

    private static async Task<IResult> ListRoles(
        ClaimsPrincipal actor,
        IAuthorizationStore store,
        CancellationToken cancellationToken)
    {
        var organizationId = TryGuid(actor, AuthorizationClaims.OrganizationId);
        if (organizationId is null)
        {
            return Results.Ok(Array.Empty<RoleResponse>());
        }

        var roles = await store.ListRolesAsync(organizationId.Value, cancellationToken);
        return Results.Ok(roles.Select(ToRole).ToArray());
    }

    private static IResult ListPermissionCatalog() =>
        Results.Ok(PermissionCatalog.All.Select(code => new PermissionCatalogItem(code, PermissionCatalog.DomainGroup(code))).ToArray());

    private static async Task<IResult> CreateRole(
        ClaimsPrincipal actor,
        [FromBody] CreateRoleRequest request,
        AuthorizationAdministrationService administration,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AuthorizationScopeType>(request.ScopeType, ignoreCase: true, out var scope))
        {
            return AuthorizationError.InvalidRequest("invalid-scope", "ScopeType must be Organization or Property.").ToHttp();
        }

        var organizationId = request.OrganizationId == Guid.Empty
            ? TryGuid(actor, AuthorizationClaims.OrganizationId) ?? Guid.Empty
            : request.OrganizationId;
        var result = await administration.CreateRoleAsync(
            organizationId,
            request.Name,
            request.Code,
            scope,
            ActorUserId(actor),
            TryGuid(actor, AuthorizationClaims.OrganizationId),
            TryGuid(actor, AuthorizationClaims.PropertyId),
            cancellationToken);
        return result.IsSuccess ? Results.Ok(ToRole(result.Value!)) : result.Error!.ToHttp();
    }

    private static async Task<IResult> SetRoleActive(
        Guid id,
        ClaimsPrincipal actor,
        [FromBody] SetActiveRequest request,
        AuthorizationAdministrationService administration,
        CancellationToken cancellationToken)
    {
        var result = await administration.SetRoleActiveAsync(
            id,
            request.IsActive,
            ActorUserId(actor),
            TryGuid(actor, AuthorizationClaims.OrganizationId),
            TryGuid(actor, AuthorizationClaims.PropertyId),
            cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToHttp();
    }

    private static async Task<IResult> ReplaceRolePermissions(
        Guid id,
        ClaimsPrincipal actor,
        [FromBody] ReplacePermissionsRequest request,
        AuthorizationAdministrationService administration,
        CancellationToken cancellationToken)
    {
        var result = await administration.ReplaceRolePermissionsAsync(
            id,
            request.PermissionCodes,
            ActorUserId(actor),
            TryGuid(actor, AuthorizationClaims.OrganizationId),
            TryGuid(actor, AuthorizationClaims.PropertyId),
            cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.Error!.ToHttp();
    }

    private static string? ActorUserId(ClaimsPrincipal actor) =>
        actor.FindFirstValue(ClaimTypes.NameIdentifier);

    private static Guid? TryGuid(ClaimsPrincipal actor, string claimType) =>
        Guid.TryParse(actor.FindFirstValue(claimType), out var value) ? value : null;

    private static async Task<IReadOnlyList<MembershipSummary>> ToMembershipsAsync(
        IReadOnlyList<UserMembership> memberships,
        IWorkforceStore workforce,
        CancellationToken cancellationToken)
    {
        var items = new List<MembershipSummary>(memberships.Count);
        foreach (var membership in memberships)
        {
            items.Add(await ToMembershipAsync(membership, workforce, cancellationToken));
        }

        return items;
    }

    private static async Task<MembershipSummary> ToMembershipAsync(
        UserMembership membership,
        IWorkforceStore workforce,
        CancellationToken cancellationToken)
    {
        var organization = await workforce.GetOrganizationAsync(membership.OrganizationId, cancellationToken);
        string? propertyName = null;
        if (membership.PropertyId is Guid propertyId)
        {
            var property = await workforce.GetPropertyAsync(propertyId, cancellationToken);
            propertyName = property?.Name;
        }

        return ToMembership(
            membership,
            organization?.Name,
            propertyName,
            membership.DepartmentScopes.Select(item => item.DepartmentId).OrderBy(id => id).ToArray());
    }

    private static MembershipSummary ToMembership(
        UserMembership membership,
        string? organizationName = null,
        string? propertyName = null,
        IReadOnlyList<Guid>? departmentIds = null) =>
        new(
            membership.Id,
            membership.OrganizationId,
            organizationName,
            membership.PropertyId,
            propertyName,
            membership.IsActive,
            membership.ScopeType.ToString(),
            membership.RoleAssignments.Select(item => item.RoleId).ToArray(),
            departmentIds ?? membership.DepartmentScopes.Select(item => item.DepartmentId).OrderBy(id => id).ToArray());

    private static RoleResponse ToRole(AuthorizationRole role) =>
        new(
            role.Id,
            role.OrganizationId,
            role.Name,
            role.Code,
            role.ScopeType.ToString(),
            role.IsSystemTemplate,
            role.IsActive,
            role.Permissions.Select(item => item.PermissionCode).OrderBy(code => code, StringComparer.Ordinal).ToArray());
}

public sealed record AuthorizationUserListItem(
    string Id,
    string? Email,
    bool LockedOut,
    Guid? EmployeeId,
    IReadOnlyList<MembershipSummary> Memberships,
    IReadOnlyList<string> EffectivePermissions);

public sealed record MembershipSummary(
    Guid Id,
    Guid OrganizationId,
    string? OrganizationName,
    Guid? PropertyId,
    string? PropertyName,
    bool IsActive,
    string ScopeType,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<Guid> DepartmentIds);

public sealed record RoleResponse(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string Code,
    string ScopeType,
    bool IsSystemTemplate,
    bool IsActive,
    IReadOnlyList<string> PermissionCodes);

public sealed record PermissionCatalogItem(string Code, string Domain);

public sealed record CreateAuthorizationUserRequest(string Email, string Password, Guid? EmployeeId);

public sealed record CreateMembershipRequest(Guid OrganizationId, Guid? PropertyId);

public sealed record SetActiveRequest(bool IsActive);

public sealed record CreateRoleRequest(Guid OrganizationId, string Name, string Code, string ScopeType);

public sealed record ReplacePermissionsRequest(IReadOnlyList<string> PermissionCodes);

public sealed record ReplaceDepartmentScopesRequest(IReadOnlyList<Guid>? DepartmentIds);

internal static class AuthorizationHttpResults
{
    public static IResult ToHttp(this AuthorizationError error)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };
        if (error.Errors is { Count: > 0 })
        {
            extensions["errors"] = error.Errors;
        }

        return Results.Problem(
            title: error.Title,
            detail: error.Detail,
            statusCode: error.StatusCode,
            extensions: extensions);
    }
}
