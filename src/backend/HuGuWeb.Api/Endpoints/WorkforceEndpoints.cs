using HuGuWeb.Api.Authorization;
using HuGuWeb.Workforce.Application;
using Microsoft.AspNetCore.Mvc;

namespace HuGuWeb.Api.Endpoints;

public static class WorkforceEndpoints
{
    public static IEndpointRouteBuilder MapWorkforceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/workforce")
            .WithTags("Workforce")
            .RequireAuthorization(AuthorizationPolicies.WorkforceRead);

        group.MapGet("/departments", ListDepartments)
            .WithName("ListDepartments");
        group.MapPost("/departments", CreateDepartment)
            .WithName("CreateDepartment")
            .RequireAuthorization(AuthorizationPolicies.WorkforceManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPatch("/departments/{id:guid}", UpdateDepartment)
            .WithName("UpdateDepartment")
            .RequireAuthorization(AuthorizationPolicies.WorkforceManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        group.MapGet("/positions", ListPositions)
            .WithName("ListPositions");
        group.MapPost("/positions", CreatePosition)
            .WithName("CreatePosition")
            .RequireAuthorization(AuthorizationPolicies.WorkforceManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPatch("/positions/{id:guid}", UpdatePosition)
            .WithName("UpdatePosition")
            .RequireAuthorization(AuthorizationPolicies.WorkforceManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        group.MapGet("/active", ListActiveWorkforce)
            .WithName("ListActiveWorkforce");
        group.MapGet("/employees", ListEmployees)
            .WithName("ListWorkforceEmployees");
        group.MapGet("/employees/{id:guid}", GetEmployee)
            .WithName("GetEmployee");
        group.MapPost("/employees/hire", HireEmployee)
            .WithName("HireEmployee")
            .RequireAuthorization(AuthorizationPolicies.WorkforceManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/employees/{id:guid}/transfer", TransferEmployee)
            .WithName("TransferEmployee")
            .RequireAuthorization(AuthorizationPolicies.WorkforceManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/employees/{id:guid}/end-employment", EndEmployment)
            .WithName("EndEmployment")
            .RequireAuthorization(AuthorizationPolicies.WorkforceManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static async Task<IResult> ListDepartments(
        MaintainDepartmentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ListAsync(cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> CreateDepartment(
        [FromBody] CreateDepartmentRequest request,
        MaintainDepartmentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.CreateAsync(new CreateDepartmentCommand(request.Name, request.Code), cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/workforce/departments/{result.Value!.Id}", result.Value)
            : result.Error!.ToHttp();
    }

    private static async Task<IResult> UpdateDepartment(
        Guid id,
        [FromBody] PatchNamedRecordRequest request,
        MaintainDepartmentsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.UpdateAsync(
            new UpdateDepartmentCommand(
                id,
                request.Name,
                request.Code,
                request.Name is not null || request.Code is not null,
                request.IsActive),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ListPositions(
        MaintainPositionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ListAsync(cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> CreatePosition(
        [FromBody] CreatePositionRequest request,
        MaintainPositionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.CreateAsync(
            new CreatePositionCommand(request.Name, request.Code, request.DepartmentIds),
            cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/api/workforce/positions/{result.Value!.Id}", result.Value)
            : result.Error!.ToHttp();
    }

    private static async Task<IResult> UpdatePosition(
        Guid id,
        [FromBody] PatchNamedRecordRequest request,
        MaintainPositionsUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.UpdateAsync(
            new UpdatePositionCommand(
                id,
                request.Name,
                request.Code,
                request.Name is not null || request.Code is not null,
                request.IsActive,
                request.DepartmentIds),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ListActiveWorkforce(
        ActiveWorkforceQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ListEmployees(
        EmployeeDirectoryQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetEmployee(
        Guid id,
        EmployeeHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(id, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> HireEmployee(
        [FromBody] HireEmployeeRequest request,
        HireEmployeeUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new HireEmployeeCommand(
                request.GivenName,
                request.FamilyName,
                request.EmploymentStartDate,
                request.DepartmentId,
                request.PositionId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Created($"/api/workforce/employees/{result.Value!.EmployeeId}", result.Value)
            : result.Error!.ToHttp();
    }

    private static async Task<IResult> TransferEmployee(
        Guid id,
        [FromBody] TransferEmployeeRequest request,
        TransferEmployeeUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(
            new TransferEmployeeCommand(id, request.DepartmentId, request.PositionId, request.EffectiveDate),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> EndEmployment(
        Guid id,
        [FromBody] EndEmploymentRequest request,
        EndEmploymentUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(new EndEmploymentCommand(id, request.EndDate), cancellationToken);
        return result.ToHttp();
    }
}

public sealed record CreateDepartmentRequest(string Name, string? Code);

public sealed record CreatePositionRequest(string Name, string? Code, IReadOnlyList<Guid>? DepartmentIds);

public sealed record PatchNamedRecordRequest(
    string? Name,
    string? Code,
    bool? IsActive,
    IReadOnlyList<Guid>? DepartmentIds);

public sealed record HireEmployeeRequest(
    string GivenName,
    string FamilyName,
    string? PersonnelNumber,
    DateOnly EmploymentStartDate,
    Guid DepartmentId,
    Guid PositionId);

public sealed record TransferEmployeeRequest(Guid DepartmentId, Guid PositionId, DateOnly EffectiveDate);

public sealed record EndEmploymentRequest(DateOnly EndDate);

internal static class WorkforceHttpResults
{
    public static IResult ToHttp<T>(this WorkforceResult<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : result.Error!.ToHttp();

    public static IResult ToHttp(this WorkforceError error)
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
