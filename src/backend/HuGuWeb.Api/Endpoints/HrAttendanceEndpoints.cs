using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.Workforce.Application;
using Microsoft.AspNetCore.Mvc;

namespace HuGuWeb.Api.Endpoints;

public static class HrAttendanceEndpoints
{
    public static IEndpointRouteBuilder MapHrAttendanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var attendance = endpoints.MapGroup("/api/hr/attendance")
            .WithTags("HR Attendance")
            .RequireAuthorization(AuthorizationPolicies.HrAttendanceRead);

        attendance.MapGet("/monthly", GetAttendanceMonth)
            .WithName("GetHrAttendanceMonth");
        attendance.MapGet("/{employmentId:guid}/{date}/history", GetAttendanceHistory)
            .WithName("GetHrAttendanceCorrectionHistory");
        attendance.MapPut("/{employmentId:guid}/{date}/correction", SetAttendanceCorrection)
            .WithName("SetHrAttendanceCorrection")
            .RequireAuthorization(AuthorizationPolicies.HrAttendanceManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        attendance.MapDelete("/{employmentId:guid}/{date}/correction", ClearAttendanceCorrection)
            .WithName("ClearHrAttendanceCorrection")
            .RequireAuthorization(AuthorizationPolicies.HrAttendanceManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static async Task<IResult> GetAttendanceMonth(
        int year,
        int month,
        Guid? departmentId,
        string? search,
        ClaimsPrincipal user,
        GetAttendanceMonthQuery query,
        EmployeeTenantGuard tenant,
        MembershipDepartmentAccess departmentAccess,
        CancellationToken cancellationToken)
    {
        var allowedDepartments = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await query.ExecuteAsync(
            year,
            month,
            departmentId,
            search,
            tenant.ScopedPropertyId(user),
            allowedDepartments,
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetAttendanceHistory(
        Guid employmentId,
        DateOnly date,
        ClaimsPrincipal user,
        GetAttendanceCorrectionHistoryQuery query,
        EmployeeTenantGuard tenant,
        MembershipDepartmentAccess departmentAccess,
        CancellationToken cancellationToken)
    {
        var allowedDepartments = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await query.ExecuteAsync(
            employmentId,
            date,
            tenant.ScopedPropertyId(user),
            allowedDepartments,
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> SetAttendanceCorrection(
        Guid employmentId,
        DateOnly date,
        ClaimsPrincipal user,
        [FromBody] SetAttendanceCorrectionRequest request,
        SetAttendanceCorrectionUseCase useCase,
        EmployeeTenantGuard tenant,
        MembershipDepartmentAccess departmentAccess,
        CancellationToken cancellationToken)
    {
        var allowedDepartments = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await useCase.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId,
                date,
                request.Kind,
                request.Reason,
                ActorUserId(user),
                tenant.ScopedPropertyId(user),
                allowedDepartments),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ClearAttendanceCorrection(
        Guid employmentId,
        DateOnly date,
        ClaimsPrincipal user,
        ClearAttendanceCorrectionUseCase useCase,
        EmployeeTenantGuard tenant,
        MembershipDepartmentAccess departmentAccess,
        CancellationToken cancellationToken)
    {
        var allowedDepartments = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await useCase.ExecuteAsync(
            new ClearAttendanceCorrectionCommand(
                employmentId,
                date,
                ActorUserId(user),
                tenant.ScopedPropertyId(user),
                allowedDepartments),
            cancellationToken);
        return result.ToHttp();
    }

    private static string ActorUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}

public sealed record SetAttendanceCorrectionRequest(string? Kind, string? Reason);
