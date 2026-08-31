using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Http;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HuGuWeb.Api.Endpoints;

public static class HrLeaveRequestEndpoints
{
    public static IEndpointRouteBuilder MapHrLeaveRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var mine = endpoints.MapGroup("/api/hr/my/leave-requests")
            .WithTags("HR Leave Requests")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveRequest);

        mine.MapGet("/", ListMine).WithName("ListMyLeaveRequests");
        mine.MapGet("/{leaveRequestId:guid}", GetMine).WithName("GetMyLeaveRequest");
        mine.MapPost("/", CreateMine)
            .WithName("CreateMyLeaveRequest")
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        mine.MapPost("/preview", PreviewMine)
            .WithName("PreviewMyLeaveRequest")
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        mine.MapPost("/{leaveRequestId:guid}/withdraw", WithdrawMine)
            .WithName("WithdrawMyLeaveRequest")
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        endpoints.MapGet("/api/hr/my/leave", GetMyLeaveCatalog)
            .WithTags("HR Leave Requests")
            .WithName("GetMyLeaveCatalog")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveRequest);

        var managed = endpoints.MapGroup("/api/hr/leave-requests")
            .WithTags("HR Leave Requests")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveRead);

        managed.MapGet("/", ListManaged).WithName("ListHrLeaveRequests");
        managed.MapGet("/{leaveRequestId:guid}", GetManaged).WithName("GetHrLeaveRequest");
        managed.MapPost("/{leaveRequestId:guid}/department-approve", DepartmentApprove)
            .WithName("DepartmentApproveLeaveRequest")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveApprove)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        managed.MapPost("/{leaveRequestId:guid}/reject", Reject)
            .WithName("RejectLeaveRequest")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveApprove)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        managed.MapPost("/{leaveRequestId:guid}/approve", HrApprove)
            .WithName("HrApproveLeaveRequest")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        managed.MapPost("/{leaveRequestId:guid}/cancel-approved", CancelApproved)
            .WithName("CancelApprovedLeaveRequest")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static async Task<IResult> GetMyLeaveCatalog(
        ClaimsPrincipal user,
        MyLeaveSelfServiceQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(LinkedEmployeeId(user), cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ListMine(
        ClaimsPrincipal user,
        int? page,
        int? pageSize,
        LeaveRequestQuery query,
        CancellationToken cancellationToken)
    {
        var linked = LinkedEmployeeId(user);
        if (linked is null)
        {
            return WorkforceError.LeaveRequestAccountLinkRequired().ToHttp();
        }

        var result = await query.ListMineAsync(
            linked.Value,
            ListQueryLimits.ClampPage(page ?? 1),
            ListQueryLimits.ClampPageSize(pageSize ?? ListQueryLimits.DefaultPageSize),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetMine(
        Guid leaveRequestId,
        ClaimsPrincipal user,
        LeaveRequestQuery query,
        CancellationToken cancellationToken)
    {
        var linked = LinkedEmployeeId(user);
        if (linked is null)
        {
            return WorkforceError.LeaveRequestAccountLinkRequired().ToHttp();
        }

        var result = await query.GetMineAsync(linked.Value, leaveRequestId, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> CreateMine(
        ClaimsPrincipal user,
        [FromBody] CreateMyLeaveRequestRequest body,
        CreateMyLeaveRequestUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new CreateMyLeaveRequestCommand(
                LinkedEmployeeId(user),
                body.LeaveTypeId,
                body.StartDate,
                body.EndDate,
                body.RequestedAmount,
                body.Reason,
                ActorUserId(user)),
            cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/hr/my/leave-requests/{result.Value!.Id}", result.Value)
            : result.Error!.ToHttp();
    }

    private static async Task<IResult> PreviewMine(
        ClaimsPrincipal user,
        [FromBody] PreviewMyLeaveRequestRequest body,
        PreviewLeaveRequestUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteMineAsync(
            new PreviewMyLeaveRequestCommand(
                LinkedEmployeeId(user),
                body.LeaveTypeId,
                body.StartDate,
                body.EndDate,
                body.RequestedAmount),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> WithdrawMine(
        Guid leaveRequestId,
        ClaimsPrincipal user,
        [FromBody] LeaveRequestNoteRequest? body,
        LeaveRequestActionUseCase useCase,
        CancellationToken cancellationToken)
    {
        var linked = LinkedEmployeeId(user);
        if (linked is null)
        {
            return WorkforceError.LeaveRequestAccountLinkRequired().ToHttp();
        }

        var result = await useCase.WithdrawMineAsync(
            linked.Value,
            leaveRequestId,
            body?.Note,
            ActorUserId(user),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ListManaged(
        ClaimsPrincipal user,
        LeaveRequestStatus? status,
        LeaveRequestApprovalStage? approvalStage,
        Guid? leaveTypeId,
        Guid? departmentId,
        DateOnly? from,
        DateOnly? to,
        string? search,
        int? page,
        int? pageSize,
        LeaveRequestQuery query,
        MembershipDepartmentAccess departmentAccess,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        var allowed = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await query.ListManagedAsync(
            new LeaveRequestListFilter(
                tenant.ScopedPropertyId(user),
                allowed,
                status,
                approvalStage,
                leaveTypeId,
                departmentId,
                from,
                to,
                search,
                ListQueryLimits.ClampPage(page ?? 1),
                ListQueryLimits.ClampPageSize(pageSize ?? ListQueryLimits.DefaultPageSize)),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetManaged(
        Guid leaveRequestId,
        ClaimsPrincipal user,
        LeaveRequestQuery query,
        MembershipDepartmentAccess departmentAccess,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        var allowed = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await query.GetManagedAsync(
            leaveRequestId,
            tenant.ScopedPropertyId(user),
            allowed,
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> DepartmentApprove(
        Guid leaveRequestId,
        ClaimsPrincipal user,
        [FromBody] LeaveRequestNoteRequest? body,
        LeaveRequestActionUseCase useCase,
        MembershipDepartmentAccess departmentAccess,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        var allowed = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await useCase.DepartmentApproveAsync(
            leaveRequestId,
            body?.Note,
            ActorUserId(user),
            tenant.ScopedPropertyId(user),
            allowed,
            CanApprove(user),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> Reject(
        Guid leaveRequestId,
        ClaimsPrincipal user,
        [FromBody] LeaveRequestNoteRequest? body,
        LeaveRequestActionUseCase useCase,
        MembershipDepartmentAccess departmentAccess,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        if (!CanApprove(user) && !CanManage(user))
        {
            return WorkforceError.LeaveRequestApprovalPermissionDenied().ToHttp();
        }

        var allowed = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await useCase.RejectAsync(
            leaveRequestId,
            body?.Note,
            ActorUserId(user),
            tenant.ScopedPropertyId(user),
            allowed,
            CanApprove(user),
            CanManage(user),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> HrApprove(
        Guid leaveRequestId,
        ClaimsPrincipal user,
        [FromBody] HrApproveLeaveRequestRequest body,
        LeaveRequestActionUseCase useCase,
        MembershipDepartmentAccess departmentAccess,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        var allowed = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await useCase.HrApproveAsync(
            leaveRequestId,
            body.FinalAmount,
            body.Note,
            ActorUserId(user),
            tenant.ScopedPropertyId(user),
            allowed,
            CanManage(user),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> CancelApproved(
        Guid leaveRequestId,
        ClaimsPrincipal user,
        [FromBody] CancelApprovedLeaveRequestRequest body,
        LeaveRequestActionUseCase useCase,
        MembershipDepartmentAccess departmentAccess,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        var allowed = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await useCase.CancelApprovedAsync(
            leaveRequestId,
            body.Reason,
            ActorUserId(user),
            tenant.ScopedPropertyId(user),
            allowed,
            CanManage(user),
            cancellationToken);
        return result.ToHttp();
    }

    private static Guid? LinkedEmployeeId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(AuthorizationClaims.EmployeeId), out var id) && id != Guid.Empty
            ? id
            : null;

    private static string ActorUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    private static bool CanApprove(ClaimsPrincipal user) =>
        user.HasClaim(HrLeavePermissions.ClaimType, HrLeavePermissions.Approve)
        || user.HasClaim(HrLeavePermissions.ClaimType, HrLeavePermissions.Manage);

    private static bool CanManage(ClaimsPrincipal user) =>
        user.HasClaim(HrLeavePermissions.ClaimType, HrLeavePermissions.Manage);
}

public sealed record CreateMyLeaveRequestRequest(
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal RequestedAmount,
    string? Reason);

public sealed record PreviewMyLeaveRequestRequest(
    Guid? LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? RequestedAmount);

public sealed record LeaveRequestNoteRequest(string? Note);

public sealed record HrApproveLeaveRequestRequest(decimal FinalAmount, string? Note);

public sealed record CancelApprovedLeaveRequestRequest(string? Reason);
