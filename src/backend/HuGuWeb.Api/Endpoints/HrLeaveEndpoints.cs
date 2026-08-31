using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HuGuWeb.Api.Endpoints;

public static class HrLeaveEndpoints
{
    public static IEndpointRouteBuilder MapHrLeaveEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var types = endpoints.MapGroup("/api/hr/leave-types")
            .WithTags("HR Leave")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveRead);

        types.MapGet("/", ListLeaveTypes)
            .WithName("ListHrLeaveTypes");
        types.MapPost("/", CreateLeaveType)
            .WithName("CreateHrLeaveType")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        types.MapPatch("/{leaveTypeId:guid}", UpdateLeaveType)
            .WithName("UpdateHrLeaveType")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        var employees = endpoints.MapGroup("/api/hr/employees")
            .WithTags("HR Leave")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveRead);

        employees.MapGet("/{employeeId:guid}/leave", GetEmployeeLeave)
            .WithName("GetHrEmployeeLeave");
        employees.MapPost("/{employeeId:guid}/leave-entitlements", CreateLeaveEntitlement)
            .WithName("CreateHrLeaveEntitlement")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        employees.MapPost("/{employeeId:guid}/leave-records", CreateLeaveRecord)
            .WithName("CreateHrLeaveRecord")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        employees.MapPost("/{employeeId:guid}/leave-records/{recordId:guid}/cancel", CancelLeaveRecord)
            .WithName("CancelHrLeaveRecord")
            .RequireAuthorization(AuthorizationPolicies.HrLeaveManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static async Task<IResult> ListLeaveTypes(
        bool? activeOnly,
        LeaveTypeAdminUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ListAsync(activeOnly ?? false, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> CreateLeaveType(
        ClaimsPrincipal user,
        [FromBody] CreateLeaveTypeRequest request,
        LeaveTypeAdminUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.CreateAsync(
            new CreateLeaveTypeCommand(
                request.Code,
                request.Name,
                request.TracksBalance,
                ActorUserId(user),
                request.DefaultRequestAmount),
            cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/hr/leave-types/{result.Value!.Id}", result.Value)
            : result.Error!.ToHttp();
    }

    private static async Task<IResult> UpdateLeaveType(
        Guid leaveTypeId,
        ClaimsPrincipal user,
        [FromBody] UpdateLeaveTypeRequest request,
        LeaveTypeAdminUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.UpdateAsync(
            new UpdateLeaveTypeCommand(
                leaveTypeId,
                request.Name,
                request.TracksBalance,
                request.IsActive,
                ActorUserId(user),
                request.DefaultRequestAmount,
                request.DefaultRequestAmountSpecified),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetEmployeeLeave(
        Guid employeeId,
        Guid? employmentId,
        ClaimsPrincipal user,
        EmployeeLeaveQuery query,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, employeeId, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var result = await query.ExecuteAsync(employeeId, employmentId, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> CreateLeaveEntitlement(
        Guid employeeId,
        ClaimsPrincipal user,
        [FromBody] CreateLeaveEntitlementRequest request,
        RecordLeaveEntitlementUseCase useCase,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, employeeId, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var result = await useCase.ExecuteAsync(
            new RecordLeaveEntitlementCommand(
                employeeId,
                request.EmploymentId,
                request.LeaveTypeId,
                request.EffectiveDate,
                request.Amount,
                request.Source,
                request.Note,
                ActorUserId(user)),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> CreateLeaveRecord(
        Guid employeeId,
        ClaimsPrincipal user,
        [FromBody] CreateLeaveRecordRequest request,
        RecordLeaveUseCase useCase,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, employeeId, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var result = await useCase.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId,
                request.EmploymentId,
                request.LeaveTypeId,
                request.StartDate,
                request.EndDate,
                request.Amount,
                request.Note,
                ActorUserId(user)),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> CancelLeaveRecord(
        Guid employeeId,
        Guid recordId,
        ClaimsPrincipal user,
        [FromBody] CancelLeaveRecordRequest request,
        CancelLeaveRecordUseCase useCase,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, employeeId, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var result = await useCase.ExecuteAsync(
            new CancelLeaveRecordCommand(employeeId, recordId, request.CancellationReason, ActorUserId(user)),
            cancellationToken);
        return result.ToHttp();
    }

    private static string ActorUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}

public sealed record CreateLeaveTypeRequest(
    string? Code,
    string? Name,
    bool TracksBalance,
    decimal? DefaultRequestAmount = null);

public sealed class UpdateLeaveTypeRequest
{
    public string? Name { get; set; }
    public bool? TracksBalance { get; set; }
    public bool? IsActive { get; set; }

    private decimal? _defaultRequestAmount;

    public bool DefaultRequestAmountSpecified { get; private set; }

    public decimal? DefaultRequestAmount
    {
        get => _defaultRequestAmount;
        set
        {
            _defaultRequestAmount = value;
            DefaultRequestAmountSpecified = true;
        }
    }
}

public sealed record CreateLeaveEntitlementRequest(
    Guid? EmploymentId,
    Guid LeaveTypeId,
    DateOnly EffectiveDate,
    decimal Amount,
    LeaveEntitlementSource Source,
    string? Note);

public sealed record CreateLeaveRecordRequest(
    Guid? EmploymentId,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Amount,
    string? Note);

public sealed record CancelLeaveRecordRequest(string? CancellationReason);
