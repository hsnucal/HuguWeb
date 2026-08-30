using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HuGuWeb.Api.Endpoints;

public static class HrScheduleEndpoints
{
    public static IEndpointRouteBuilder MapHrScheduleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var definitions = endpoints.MapGroup("/api/hr/shift-definitions")
            .WithTags("HR Schedule")
            .RequireAuthorization(AuthorizationPolicies.HrShiftDefinitionRead);

        definitions.MapGet("/", ListShiftDefinitions)
            .WithName("ListHrShiftDefinitions");
        definitions.MapPost("/", CreateShiftDefinition)
            .WithName("CreateHrShiftDefinition")
            .RequireAuthorization(AuthorizationPolicies.HrShiftDefinitionManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        definitions.MapPatch("/{shiftDefinitionId:guid}", UpdateShiftDefinition)
            .WithName("UpdateHrShiftDefinition")
            .RequireAuthorization(AuthorizationPolicies.HrShiftDefinitionManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        var schedule = endpoints.MapGroup("/api/hr/schedule")
            .WithTags("HR Schedule")
            .RequireAuthorization(AuthorizationPolicies.HrScheduleRead);

        schedule.MapGet("/week", GetScheduleWeek)
            .WithName("GetHrScheduleWeek");
        schedule.MapPost("/bulk", BulkSchedule)
            .WithName("BulkHrSchedule")
            .RequireAuthorization(AuthorizationPolicies.HrScheduleManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        schedule.MapPost("/copy-week/preview", PreviewCopyScheduleWeek)
            .WithName("PreviewHrScheduleCopyWeek")
            .RequireAuthorization(AuthorizationPolicies.HrScheduleManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        schedule.MapPost("/copy-week", CopyScheduleWeek)
            .WithName("CopyHrScheduleWeek")
            .RequireAuthorization(AuthorizationPolicies.HrScheduleManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        var employees = endpoints.MapGroup("/api/hr/employees")
            .WithTags("HR Schedule")
            .RequireAuthorization(AuthorizationPolicies.HrScheduleRead);

        employees.MapGet("/{employeeId:guid}/schedule", GetScheduleRange)
            .WithName("GetHrEmployeeScheduleRange");
        employees.MapGet("/{employeeId:guid}/schedule/{date}", GetScheduleState)
            .WithName("GetHrEmployeeScheduleState");
        employees.MapPut("/{employeeId:guid}/schedule/{date}", UpsertSchedule)
            .WithName("UpsertHrEmployeeSchedule")
            .RequireAuthorization(AuthorizationPolicies.HrScheduleManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        employees.MapPost("/{employeeId:guid}/schedule/{date}/clear", ClearSchedule)
            .WithName("ClearHrEmployeeSchedule")
            .RequireAuthorization(AuthorizationPolicies.HrScheduleManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static async Task<IResult> ListShiftDefinitions(
        bool? activeOnly,
        ShiftDefinitionAdminUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ListAsync(activeOnly ?? false, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> CreateShiftDefinition(
        ClaimsPrincipal user,
        [FromBody] CreateShiftDefinitionRequest request,
        ShiftDefinitionAdminUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.CreateAsync(
            new CreateShiftDefinitionCommand(
                request.Code,
                request.Name,
                request.StartLocalTime,
                request.EndLocalTime,
                request.EndsNextDay,
                request.BreakMinutes,
                ActorUserId(user)),
            cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/hr/shift-definitions/{result.Value!.Id}", result.Value)
            : result.Error!.ToHttp();
    }

    private static async Task<IResult> UpdateShiftDefinition(
        Guid shiftDefinitionId,
        ClaimsPrincipal user,
        [FromBody] UpdateShiftDefinitionRequest request,
        ShiftDefinitionAdminUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.UpdateAsync(
            new UpdateShiftDefinitionCommand(
                shiftDefinitionId,
                request.Name,
                request.StartLocalTime,
                request.EndLocalTime,
                request.EndsNextDay,
                request.BreakMinutes,
                request.IsActive,
                ActorUserId(user)),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetScheduleWeek(
        DateOnly weekStart,
        Guid? departmentId,
        ClaimsPrincipal user,
        GetScheduleWeekQuery query,
        EmployeeTenantGuard tenant,
        MembershipDepartmentAccess departmentAccess,
        CancellationToken cancellationToken)
    {
        var allowedDepartments = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await query.ExecuteAsync(
            weekStart,
            departmentId,
            tenant.ScopedPropertyId(user),
            allowedDepartments,
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> BulkSchedule(
        ClaimsPrincipal user,
        [FromBody] BulkScheduleRequest request,
        BulkScheduleUseCase useCase,
        EmployeeTenantGuard tenant,
        MembershipDepartmentAccess departmentAccess,
        CancellationToken cancellationToken)
    {
        if (request.Operations is null || request.Operations.Count == 0)
        {
            return WorkforceError.ScheduleValidationField(
                ScheduleValidation.Fields.Operations,
                ScheduleValidation.Codes.ScheduleBulkFailed,
                "At least one schedule operation is required.").ToHttp();
        }

        var operations = new List<BulkScheduleOperation>(request.Operations.Count);
        for (var index = 0; index < request.Operations.Count; index++)
        {
            var item = request.Operations[index];
            if (item.Clear)
            {
                operations.Add(new BulkScheduleOperation(
                    item.EmployeeId,
                    item.Date,
                    Clear: true,
                    Kind: null,
                    ShiftDefinitionId: null,
                    item.Note));
                continue;
            }

            if (!TryParseKind(item.Kind, out var kind))
            {
                return WorkforceError.ScheduleBulkOperationFailed(
                    index,
                    item.EmployeeId,
                    item.Date,
                    WorkforceError.ScheduleValidationField(
                        ScheduleValidation.Fields.Kind,
                        ScheduleValidation.Codes.ScheduleInvalidKind,
                        "Schedule kind must be Shift or RestDay.")).ToHttp();
            }

            operations.Add(new BulkScheduleOperation(
                item.EmployeeId,
                item.Date,
                Clear: false,
                kind,
                item.ShiftDefinitionId,
                item.Note));
        }

        var allowedDepartments = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await useCase.ExecuteAsync(
            new BulkScheduleCommand(
                operations,
                ActorUserId(user),
                tenant.ScopedPropertyId(user),
                allowedDepartments),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> PreviewCopyScheduleWeek(
        ClaimsPrincipal user,
        [FromBody] CopyScheduleWeekRequest request,
        CopyScheduleWeekUseCase useCase,
        EmployeeTenantGuard tenant,
        MembershipDepartmentAccess departmentAccess,
        CancellationToken cancellationToken)
    {
        var allowedDepartments = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await useCase.PreviewAsync(
            new CopyScheduleWeekCommand(
                request.TargetWeekStart,
                request.DepartmentId,
                ActorUserId(user),
                tenant.ScopedPropertyId(user),
                allowedDepartments),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> CopyScheduleWeek(
        ClaimsPrincipal user,
        [FromBody] CopyScheduleWeekRequest request,
        CopyScheduleWeekUseCase useCase,
        EmployeeTenantGuard tenant,
        MembershipDepartmentAccess departmentAccess,
        CancellationToken cancellationToken)
    {
        var allowedDepartments = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await useCase.ExecuteAsync(
            new CopyScheduleWeekCommand(
                request.TargetWeekStart,
                request.DepartmentId,
                ActorUserId(user),
                tenant.ScopedPropertyId(user),
                allowedDepartments),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetScheduleRange(
        Guid employeeId,
        DateOnly from,
        DateOnly to,
        ClaimsPrincipal user,
        GetScheduleRangeQuery query,
        EmployeeTenantGuard tenant,
        MembershipDepartmentAccess departmentAccess,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeInOrganizationAsync(user, employeeId, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var allowedDepartments = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await query.ExecuteAsync(
            employeeId,
            from,
            to,
            tenant.ScopedPropertyId(user),
            allowedDepartments,
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetScheduleState(
        Guid employeeId,
        DateOnly date,
        ClaimsPrincipal user,
        GetScheduleStateQuery query,
        EmployeeTenantGuard tenant,
        MembershipDepartmentAccess departmentAccess,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeInOrganizationAsync(user, employeeId, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var allowedDepartments = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await query.ExecuteAsync(
            employeeId,
            date,
            tenant.ScopedPropertyId(user),
            allowedDepartments,
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> UpsertSchedule(
        Guid employeeId,
        DateOnly date,
        ClaimsPrincipal user,
        [FromBody] UpsertScheduleRequest request,
        UpsertScheduleEntryUseCase useCase,
        EmployeeTenantGuard tenant,
        MembershipDepartmentAccess departmentAccess,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeInOrganizationAsync(user, employeeId, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        if (!TryParseKind(request.Kind, out var kind))
        {
            return WorkforceError.ScheduleValidationField(
                ScheduleValidation.Fields.Kind,
                ScheduleValidation.Codes.ScheduleInvalidKind,
                "Schedule kind must be Shift or RestDay.").ToHttp();
        }

        var allowedDepartments = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await useCase.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId,
                date,
                kind,
                request.ShiftDefinitionId,
                request.Note,
                ActorUserId(user),
                tenant.ScopedPropertyId(user),
                allowedDepartments),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ClearSchedule(
        Guid employeeId,
        DateOnly date,
        ClaimsPrincipal user,
        ClearScheduleEntryUseCase useCase,
        EmployeeTenantGuard tenant,
        MembershipDepartmentAccess departmentAccess,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeInOrganizationAsync(user, employeeId, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var allowedDepartments = await departmentAccess.GetAllowedDepartmentsAsync(
            MembershipDepartmentAccess.MembershipIdFromClaims(user),
            cancellationToken);
        var result = await useCase.ExecuteAsync(
            new ClearScheduleEntryCommand(
                employeeId,
                date,
                ActorUserId(user),
                tenant.ScopedPropertyId(user),
                allowedDepartments),
            cancellationToken);
        return result.ToHttp();
    }

    private static bool TryParseKind(string? kind, out ScheduleEntryKind parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(kind))
        {
            return false;
        }

        if (Enum.TryParse(kind, ignoreCase: true, out ScheduleEntryKind value)
            && (value == ScheduleEntryKind.Shift || value == ScheduleEntryKind.RestDay))
        {
            parsed = value;
            return true;
        }

        return false;
    }

    private static string ActorUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}

public sealed record CreateShiftDefinitionRequest(
    string? Code,
    string? Name,
    TimeOnly StartLocalTime,
    TimeOnly EndLocalTime,
    bool EndsNextDay,
    int BreakMinutes);

public sealed record UpdateShiftDefinitionRequest(
    string? Name,
    TimeOnly? StartLocalTime,
    TimeOnly? EndLocalTime,
    bool? EndsNextDay,
    int? BreakMinutes,
    bool? IsActive);

public sealed record UpsertScheduleRequest(string? Kind, Guid? ShiftDefinitionId, string? Note);

public sealed record BulkScheduleRequest(IReadOnlyList<BulkScheduleOperationRequest>? Operations);

public sealed record BulkScheduleOperationRequest(
    Guid EmployeeId,
    DateOnly Date,
    bool Clear,
    string? Kind,
    Guid? ShiftDefinitionId,
    string? Note);

public sealed record CopyScheduleWeekRequest(DateOnly TargetWeekStart, Guid? DepartmentId);
