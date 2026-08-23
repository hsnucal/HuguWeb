using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.RoomOperations.Application;
using HuGuWeb.RoomOperations.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HuGuWeb.Api.Endpoints;

public static class RoomOperationsEndpoints
{
    public static IEndpointRouteBuilder MapRoomOperationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/room-operations")
            .WithTags("RoomOperations")
            .RequireAuthorization(AuthorizationPolicies.RoomOperationsRead);

        group.MapGet("/rooms", ListRooms)
            .WithName("ListRoomOperationsRooms");
        group.MapGet("/rooms/{id:guid}", GetRoom)
            .WithName("GetRoomOperationsRoom");
        group.MapGet("/assignable-employees", ListAssignableEmployees)
            .WithName("ListRoomOperationsAssignableEmployees");

        group.MapPost("/rooms/{id:guid}/needs-cleaning", RequestNeedsCleaning)
            .WithName("RequestRoomNeedsCleaning")
            .RequireAuthorization(AuthorizationPolicies.RoomOperationsManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        group.MapPost("/work-items/{id:guid}/complete-cleaning", CompleteCleaning)
            .WithName("CompleteRoomCleaning")
            .RequireAuthorization(AuthorizationPolicies.RoomOperationsManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        group.MapPost("/rooms/{id:guid}/inspections", InspectRoom)
            .WithName("InspectRoom")
            .RequireAuthorization(AuthorizationPolicies.RoomOperationsInspect)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static async Task<IResult> ListRooms(
        ListRoomOperationsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetRoom(
        Guid id,
        GetRoomOperationsDetailQuery query,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(id, cancellationToken);
        return result.ToHttp(principal);
    }

    private static async Task<IResult> ListAssignableEmployees(
        ListAssignableEmployeesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> RequestNeedsCleaning(
        Guid id,
        [FromBody] NeedsCleaningRequest request,
        RequestNeedsCleaningUseCase useCase,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!TryParseUserId(principal, out var actorUserId))
        {
            return AuthenticationRequired();
        }

        if (!TryParsePriority(request.Priority, out var priority))
        {
            return RoomOperationsError.InvalidPriority().ToHttp();
        }

        var result = await useCase.ExecuteAsync(
            new RequestNeedsCleaningCommand(id, request.AssignedEmployeeId, priority, actorUserId),
            cancellationToken);
        return result.ToHttp(principal);
    }

    private static async Task<IResult> CompleteCleaning(
        Guid id,
        CompleteCleaningUseCase useCase,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!TryParseUserId(principal, out var actorUserId))
        {
            return AuthenticationRequired();
        }

        var result = await useCase.ExecuteAsync(new CompleteCleaningCommand(id, actorUserId), cancellationToken);
        return result.ToHttp(principal);
    }

    private static async Task<IResult> InspectRoom(
        Guid id,
        [FromBody] InspectRoomRequest request,
        InspectRoomUseCase useCase,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        if (!TryParseUserId(principal, out var actorUserId))
        {
            return AuthenticationRequired();
        }

        var accepted = string.Equals(request.Result, "accepted", StringComparison.OrdinalIgnoreCase);
        var rejected = string.Equals(request.Result, "rejected", StringComparison.OrdinalIgnoreCase);
        if (!accepted && !rejected)
        {
            return RoomOperationsError.InvalidRequest(
                    "invalid-request",
                    "Inspection result must be accepted or rejected.")
                .ToHttp();
        }

        var result = await useCase.ExecuteAsync(
            new InspectRoomCommand(id, accepted, request.Reason, actorUserId),
            cancellationToken);
        return result.ToHttp(principal);
    }

    private static bool TryParseUserId(ClaimsPrincipal principal, out Guid userId)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }

    private static bool TryParsePriority(string? value, out TaskPriority priority)
    {
        return Enum.TryParse(value, ignoreCase: true, out priority) && Enum.IsDefined(priority);
    }

    private static IResult AuthenticationRequired() =>
        Results.Problem(
            title: "Authentication required.",
            statusCode: StatusCodes.Status401Unauthorized);
}

public sealed record NeedsCleaningRequest(Guid AssignedEmployeeId, string Priority);

public sealed record InspectRoomRequest(string Result, string? Reason);

internal static class RoomOperationsHttpResults
{
    public static IResult ToHttp<T>(this RoomOperationsResult<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToHttp();

    public static IResult ToHttp(this RoomOperationsResult<RoomOperationsDetail> result, ClaimsPrincipal principal) =>
        result.IsSuccess
            ? Results.Ok(RoomOperationsExposure.ForCaller(result.Value!, RoomOperationsExposure.CanReadMaintenance(principal)))
            : result.Error!.ToHttp();

    public static IResult ToHttp(this RoomOperationsError error) =>
        Results.Problem(
            title: error.Title,
            detail: error.Detail,
            statusCode: error.StatusCode,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
