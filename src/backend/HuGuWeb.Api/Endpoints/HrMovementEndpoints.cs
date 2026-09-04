using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HuGuWeb.Api.Endpoints;

public static class HrMovementEndpoints
{
    public static IEndpointRouteBuilder MapHrMovementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/hr/movements")
            .WithTags("HR Movements")
            .RequireAuthorization(AuthorizationPolicies.HrMovementsRead);

        group.MapGet("/", ListMovements)
            .WithName("ListHrPersonnelMovements");
        group.MapGet("/{id:guid}", GetMovement)
            .WithName("GetHrPersonnelMovement");
        group.MapPost("/", CreateMovement)
            .WithName("CreateHrPersonnelMovement")
            .RequireAuthorization(AuthorizationPolicies.HrMovementsManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/{id:guid}/cancel", CancelMovement)
            .WithName("CancelHrPersonnelMovement")
            .RequireAuthorization(AuthorizationPolicies.HrMovementsManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static async Task<IResult> ListMovements(
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string? type,
        Guid? departmentId,
        Guid? employeeId,
        string? search,
        ClaimsPrincipal user,
        ListPersonnelMovementsQuery query,
        IAuthorizationStore authorizationStore,
        PropertyAccessService propertyAccess,
        IWorkplaceContext workplace,
        CancellationToken cancellationToken)
    {
        if (!TryParseListType(type, out var movementType, out var typeError))
        {
            return typeError!.ToHttp();
        }

        var scope = await MovementAccess.AccessiblePropertyIdsAsync(
            user,
            authorizationStore,
            propertyAccess,
            workplace.OrganizationId,
            cancellationToken);
        var result = await query.ExecuteAsync(
            new ListPersonnelMovementsFilter(
                dateFrom,
                dateTo,
                movementType,
                departmentId,
                employeeId,
                search,
                scope),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetMovement(
        Guid id,
        ClaimsPrincipal user,
        GetPersonnelMovementQuery query,
        IAuthorizationStore authorizationStore,
        PropertyAccessService propertyAccess,
        IWorkplaceContext workplace,
        CancellationToken cancellationToken)
    {
        var scope = await MovementAccess.AccessiblePropertyIdsAsync(
            user,
            authorizationStore,
            propertyAccess,
            workplace.OrganizationId,
            cancellationToken);
        var result = await query.ExecuteAsync(id, scope, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> CreateMovement(
        [FromBody] CreatePersonnelMovementRequest request,
        ClaimsPrincipal user,
        CreateWorkforceMovementUseCase useCase,
        IAuthorizationStore authorizationStore,
        PropertyAccessService propertyAccess,
        IWorkplaceContext workplace,
        CancellationToken cancellationToken)
    {
        if (!TryParsePublicType(request.Type, out var movementType, out var typeError))
        {
            return typeError!.ToHttp();
        }

        var scope = await MovementAccess.AccessiblePropertyIdsAsync(
            user,
            authorizationStore,
            propertyAccess,
            workplace.OrganizationId,
            cancellationToken);
        var result = await useCase.ExecuteAsync(
            new CreatePersonnelMovementCommand(
                EmployeeId: null,
                request.EmploymentId,
                movementType,
                request.EffectiveDate,
                request.TargetPropertyId,
                request.TargetDepartmentId,
                request.TargetPositionId,
                request.TargetManagerEmploymentId,
                request.ClearManager,
                request.Reason,
                request.Note,
                ActorUserId(user),
                scope),
            cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/hr/movements/{result.Value!.Id}", result.Value)
            : result.Error!.ToHttp();
    }

    private static async Task<IResult> CancelMovement(
        Guid id,
        [FromBody] CancelPersonnelMovementRequest request,
        ClaimsPrincipal user,
        CancelWorkforceMovementUseCase useCase,
        IAuthorizationStore authorizationStore,
        PropertyAccessService propertyAccess,
        IWorkplaceContext workplace,
        CancellationToken cancellationToken)
    {
        var scope = await MovementAccess.AccessiblePropertyIdsAsync(
            user,
            authorizationStore,
            propertyAccess,
            workplace.OrganizationId,
            cancellationToken);
        var result = await useCase.ExecuteAsync(
            new CancelPersonnelMovementCommand(id, request.Reason, ActorUserId(user), scope),
            cancellationToken);
        return result.ToHttp();
    }

    private static bool TryParsePublicType(string? value, out PersonnelMovementType type, out WorkforceError? error)
    {
        type = default;
        error = null;
        if (!Enum.TryParse(value, ignoreCase: true, out type)
            || !Enum.IsDefined(type)
            || type is PersonnelMovementType.AssignmentChange)
        {
            error = WorkforceError.MovementInvalidType();
            return false;
        }

        return true;
    }

    private static bool TryParseListType(string? value, out PersonnelMovementType? type, out WorkforceError? error)
    {
        type = null;
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Enum.TryParse<PersonnelMovementType>(value, ignoreCase: true, out var parsed) || !Enum.IsDefined(parsed))
        {
            error = WorkforceError.MovementInvalidType();
            return false;
        }

        type = parsed;
        return true;
    }

    private static string ActorUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}

public sealed record CreatePersonnelMovementRequest(
    Guid EmploymentId,
    string Type,
    DateOnly EffectiveDate,
    Guid? TargetPropertyId,
    Guid? TargetDepartmentId,
    Guid? TargetPositionId,
    Guid? TargetManagerEmploymentId,
    bool ClearManager,
    string Reason,
    string? Note);

public sealed record CancelPersonnelMovementRequest(string Reason);

internal static class MovementAccess
{
    public static async Task<IReadOnlySet<Guid>?> AccessiblePropertyIdsAsync(
        ClaimsPrincipal user,
        IAuthorizationStore store,
        PropertyAccessService propertyAccess,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new HashSet<Guid>();
        }

        var memberships = await store.ListMembershipsForUserAsync(userId, cancellationToken);
        if (memberships.Any(item =>
                item.IsActive && item.OrganizationId == organizationId && item.PropertyId is null))
        {
            return null;
        }

        var properties = await propertyAccess.ListAccessiblePropertiesAsync(userId, cancellationToken);
        return properties
            .Where(item => item.OrganizationId == organizationId)
            .Select(item => item.Id)
            .ToHashSet();
    }
}
