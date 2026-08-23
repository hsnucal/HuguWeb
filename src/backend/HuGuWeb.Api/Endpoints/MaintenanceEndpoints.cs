using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.TechnicalService.Application;
using HuGuWeb.TechnicalService.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HuGuWeb.Api.Endpoints;

public static class MaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/maintenance")
            .WithTags("Maintenance")
            .RequireAuthorization(AuthorizationPolicies.MaintenanceRead);

        group.MapGet("/issues", ListIssues)
            .WithName("ListMaintenanceIssues");
        group.MapGet("/issues/{id:guid}", GetIssue)
            .WithName("GetMaintenanceIssue");
        group.MapGet("/rooms", ListRooms)
            .WithName("ListMaintenanceRooms")
            .RequireAuthorization(AuthorizationPolicies.MaintenanceManage);
        group.MapGet("/categories", ListCategories)
            .WithName("ListMaintenanceCategories")
            .RequireAuthorization(AuthorizationPolicies.MaintenanceManage);
        group.MapGet("/assignable-employees", ListAssignableEmployees)
            .WithName("ListMaintenanceAssignableEmployees")
            .RequireAuthorization(AuthorizationPolicies.MaintenanceManage);

        group.MapPost("/issues", CreateIssue)
            .WithName("CreateMaintenanceIssue")
            .RequireAuthorization(AuthorizationPolicies.MaintenanceManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/issues/{id:guid}/assign", AssignIssue)
            .WithName("AssignMaintenanceIssue")
            .RequireAuthorization(AuthorizationPolicies.MaintenanceManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/issues/{id:guid}/priority", ChangePriority)
            .WithName("ChangeMaintenanceIssuePriority")
            .RequireAuthorization(AuthorizationPolicies.MaintenanceManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/issues/{id:guid}/blocking", ChangeBlocking)
            .WithName("ChangeMaintenanceIssueBlocking")
            .RequireAuthorization(AuthorizationPolicies.MaintenanceManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/issues/{id:guid}/start", StartWork)
            .WithName("StartMaintenanceWork")
            .RequireAuthorization(AuthorizationPolicies.MaintenanceResolve)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/issues/{id:guid}/unable-to-resolve", MarkUnableToResolve)
            .WithName("MarkMaintenanceUnableToResolve")
            .RequireAuthorization(AuthorizationPolicies.MaintenanceResolve)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/issues/{id:guid}/resume", ResumeWork)
            .WithName("ResumeMaintenanceWork")
            .RequireAuthorization(AuthorizationPolicies.MaintenanceResolve)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/issues/{id:guid}/resolve", ResolveWork)
            .WithName("ResolveMaintenanceWork")
            .RequireAuthorization(AuthorizationPolicies.MaintenanceResolve)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static async Task<IResult> ListIssues(ListIssuesQuery query, CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetIssue(
        Guid id,
        GetIssueDetailQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(id, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ListRooms(ListRoomsQuery query, CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ListCategories(ListCategoriesQuery query, CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ListAssignableEmployees(
        ListAssignableEmployeesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> CreateIssue(
        [FromBody] CreateMaintenanceIssueRequest request,
        CreateIssueUseCase useCase,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!TryParseUserId(principal, out var actorUserId))
        {
            return AuthenticationRequired();
        }

        if (!TryParsePriority(request.Priority, out var priority))
        {
            return TechnicalServiceError.InvalidPriority().ToHttp();
        }

        if (!TryParseOptionalOutage(request.OutageClassification, out var outage, out var outageError))
        {
            return outageError.ToHttp();
        }

        var result = await useCase.ExecuteAsync(
            new CreateIssueCommand(
                request.RoomId,
                request.CategoryId,
                request.Description,
                priority,
                request.AssignedEmployeeId,
                request.ReportedByEmployeeId,
                request.OriginNote,
                request.BlocksRoomUse,
                outage,
                actorUserId),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> AssignIssue(
        Guid id,
        [FromBody] AssignMaintenanceIssueRequest request,
        AssignIssueUseCase useCase,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!TryParseUserId(principal, out var actorUserId))
        {
            return AuthenticationRequired();
        }

        var result = await useCase.ExecuteAsync(
            new AssignIssueCommand(id, request.AssignedEmployeeId, request.ExpectedVersion, actorUserId),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ChangePriority(
        Guid id,
        [FromBody] ChangeMaintenancePriorityRequest request,
        ChangePriorityUseCase useCase,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!TryParseUserId(principal, out var actorUserId))
        {
            return AuthenticationRequired();
        }

        if (!TryParsePriority(request.Priority, out var priority))
        {
            return TechnicalServiceError.InvalidPriority().ToHttp();
        }

        var result = await useCase.ExecuteAsync(
            new ChangePriorityCommand(id, priority, request.ExpectedVersion, actorUserId),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ChangeBlocking(
        Guid id,
        [FromBody] ChangeMaintenanceBlockingRequest request,
        ChangeBlockingUseCase useCase,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!TryParseUserId(principal, out var actorUserId))
        {
            return AuthenticationRequired();
        }

        if (!TryParseOptionalOutage(request.OutageClassification, out var outage, out var outageError))
        {
            return outageError.ToHttp();
        }

        var result = await useCase.ExecuteAsync(
            new ChangeBlockingCommand(id, request.BlocksRoomUse, outage, request.ExpectedVersion, actorUserId),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> StartWork(
        Guid id,
        [FromBody] VersionedMaintenanceRequest request,
        StartWorkUseCase useCase,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!TryParseUserId(principal, out var actorUserId))
        {
            return AuthenticationRequired();
        }

        var result = await useCase.ExecuteAsync(
            new StartWorkCommand(id, request.ExpectedVersion, actorUserId),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> MarkUnableToResolve(
        Guid id,
        [FromBody] NoteMaintenanceRequest request,
        MarkUnableToResolveUseCase useCase,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!TryParseUserId(principal, out var actorUserId))
        {
            return AuthenticationRequired();
        }

        var result = await useCase.ExecuteAsync(
            new MarkUnableToResolveCommand(id, request.Note, request.ExpectedVersion, actorUserId),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ResumeWork(
        Guid id,
        [FromBody] VersionedMaintenanceRequest request,
        ResumeWorkUseCase useCase,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!TryParseUserId(principal, out var actorUserId))
        {
            return AuthenticationRequired();
        }

        var result = await useCase.ExecuteAsync(
            new ResumeWorkCommand(id, request.ExpectedVersion, actorUserId),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ResolveWork(
        Guid id,
        [FromBody] ResolveMaintenanceRequest request,
        ResolveWorkUseCase useCase,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!TryParseUserId(principal, out var actorUserId))
        {
            return AuthenticationRequired();
        }

        if (!TryParsePreparationImpact(request.PreparationImpact, out var impact))
        {
            return TechnicalServiceError.InvalidPreparationImpact().ToHttp();
        }

        var result = await useCase.ExecuteAsync(
            new ResolveWorkCommand(id, request.Note, impact, request.ExpectedVersion, actorUserId),
            cancellationToken);
        return result.ToHttp();
    }

    private static bool TryParseUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }

    private static bool TryParsePriority(string? value, out MaintenancePriority priority)
    {
        return Enum.TryParse(value, ignoreCase: true, out priority) && Enum.IsDefined(priority);
    }

    private static bool TryParsePreparationImpact(string? value, out PreparationImpact impact)
    {
        return Enum.TryParse(value, ignoreCase: true, out impact) && Enum.IsDefined(impact);
    }

    private static bool TryParseOptionalOutage(
        string? value,
        out OutageClassification? outage,
        out TechnicalServiceError error)
    {
        outage = null;
        error = TechnicalServiceError.InvalidBlocking();
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!Enum.TryParse(value, ignoreCase: true, out OutageClassification parsed) || !Enum.IsDefined(parsed))
        {
            return false;
        }

        outage = parsed;
        return true;
    }

    private static IResult AuthenticationRequired() =>
        Results.Problem(
            title: "Authentication required.",
            statusCode: StatusCodes.Status401Unauthorized);
}

public sealed record CreateMaintenanceIssueRequest(
    Guid RoomId,
    Guid CategoryId,
    string Description,
    string Priority,
    Guid? AssignedEmployeeId,
    Guid? ReportedByEmployeeId,
    string? OriginNote,
    bool BlocksRoomUse,
    string? OutageClassification);

public sealed record AssignMaintenanceIssueRequest(Guid AssignedEmployeeId, int ExpectedVersion);

public sealed record ChangeMaintenancePriorityRequest(string Priority, int ExpectedVersion);

public sealed record ChangeMaintenanceBlockingRequest(
    bool BlocksRoomUse,
    string? OutageClassification,
    int ExpectedVersion);

public sealed record VersionedMaintenanceRequest(int ExpectedVersion);

public sealed record NoteMaintenanceRequest(string Note, int ExpectedVersion);

public sealed record ResolveMaintenanceRequest(string Note, string PreparationImpact, int ExpectedVersion);

internal static class MaintenanceHttpResults
{
    public static IResult ToHttp<T>(this TechnicalServiceResult<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToHttp();

    public static IResult ToHttp(this TechnicalServiceError error) =>
        Results.Problem(
            title: error.Title,
            detail: error.Detail,
            statusCode: error.StatusCode,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
