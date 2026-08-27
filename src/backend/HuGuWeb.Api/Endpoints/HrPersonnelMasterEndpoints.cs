using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Context;
using HuGuWeb.Api.Identity;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HuGuWeb.Api.Endpoints;

public static class HrPersonnelMasterEndpoints
{
    public static IEndpointRouteBuilder MapHrPersonnelMasterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/hr/employees")
            .WithTags("HR Personnel Master");

        group.MapGet("/export", ExportEmployees)
            .WithName("ExportHrEmployees")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeRead);

        group.MapGet("/import/template", DownloadImportTemplate)
            .WithName("DownloadHrPersonnelImportTemplate")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeManage);

        group.MapPost("/import/preview", PreviewImport)
            .WithName("PreviewHrPersonnelImport")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeManage)
            .DisableAntiforgery()
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        group.MapPost("/import/confirm", ConfirmImport)
            .WithName("ConfirmHrPersonnelImport")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        group.MapGet("/{id:guid}/profile-history", GetProfileHistory)
            .WithName("GetHrPersonnelProfileHistory")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeRead);

        group.MapGet("/{id:guid}/erp-account", GetErpAccount)
            .WithName("GetHrEmployeeErpAccount")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeRead);

        group.MapPut("/{id:guid}/payment-profile", SavePaymentProfile)
            .WithName("SaveHrEmployeePaymentProfile")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static async Task<IResult> ExportEmployees(
        ClaimsPrincipal user,
        PersonnelExcelExportUseCase export,
        HrEmployeeDirectoryQuery directoryQuery,
        EmployeeTenantGuard tenant,
        string? search,
        Guid? departmentId,
        Guid? positionId,
        EmploymentStatus? status,
        DateOnly? startFrom,
        DateOnly? startTo,
        string? columns,
        CancellationToken cancellationToken)
    {
        var canReadSensitive = CanReadSensitive(user);
        IReadOnlyList<Guid>? scopedEmployeeIds = null;
        if (!tenant.IsOrganizationWide(user))
        {
            var directory = await directoryQuery.ExecuteAsync(canReadSensitive, cancellationToken);
            if (!directory.IsSuccess)
            {
                return directory.Error!.ToHttp();
            }

            var allowed = new List<Guid>();
            foreach (var item in directory.Value!)
            {
                if (await tenant.AllowsEmployeeAsync(user, item.EmployeeId, cancellationToken))
                {
                    allowed.Add(item.EmployeeId);
                }
            }

            scopedEmployeeIds = allowed;
        }

        var visibleColumns = string.IsNullOrWhiteSpace(columns)
            ? null
            : columns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = await export.ExecuteAsync(
            new PersonnelExportQuery(
                canReadSensitive,
                search,
                departmentId,
                positionId,
                status,
                startFrom,
                startTo,
                visibleColumns,
                scopedEmployeeIds),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!.ToHttp();
        }

        return Results.File(
            result.Value!,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"personnel-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx");
    }

    private static async Task<IResult> DownloadImportTemplate(
        PersonnelExcelImportUseCase import,
        CancellationToken cancellationToken)
    {
        var result = await import.BuildTemplateAsync(cancellationToken);
        return result.IsSuccess
            ? Results.File(
                result.Value!,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "hugu-personnel-import-template.xlsx")
            : result.Error!.ToHttp();
    }

    private static async Task<IResult> PreviewImport(
        ClaimsPrincipal user,
        HttpRequest request,
        PersonnelExcelImportUseCase import,
        IRequestActorContext actorContext,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return WorkforceError.PersonnelImportInvalidFile("Excel file is required.").ToHttp();
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            return WorkforceError.PersonnelImportInvalidFile("Excel file is required.").ToHttp();
        }

        await using var stream = file.OpenReadStream();
        var actor = RequireActor(actorContext);
        if (actor is null)
        {
            return Results.Unauthorized();
        }

        var result = await import.PreviewAsync(
            new PersonnelImportPreviewCommand(
                stream,
                file.Length,
                file.FileName,
                CanReadSensitive(user),
                actor.UserId),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ConfirmImport(
        ClaimsPrincipal user,
        [FromBody] ConfirmPersonnelImportRequest request,
        PersonnelExcelImportUseCase import,
        IRequestActorContext actorContext,
        CancellationToken cancellationToken)
    {
        var actor = RequireActor(actorContext);
        if (actor is null)
        {
            return Results.Unauthorized();
        }

        var result = await import.ConfirmAsync(
            new PersonnelImportConfirmCommand(
                request.PreviewToken,
                ToPersonnelChangeContext(actor),
                CanReadSensitive(user)),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetProfileHistory(
        Guid id,
        ClaimsPrincipal user,
        PersonnelProfileHistoryQuery query,
        EmployeeTenantGuard tenant,
        EmployeeHistoryQuery employmentHistory,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, id, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var profileHistory = await query.ExecuteAsync(id, CanReadSensitive(user), cancellationToken);
        if (!profileHistory.IsSuccess)
        {
            return profileHistory.Error!.ToHttp();
        }

        var employment = await employmentHistory.ExecuteAsync(id, cancellationToken);
        if (!employment.IsSuccess)
        {
            return employment.Error!.ToHttp();
        }

        return Results.Ok(new PersonnelHistoryResponse(profileHistory.Value!, employment.Value!.Employments));
    }

    private static async Task<IResult> GetErpAccount(
        Guid id,
        ClaimsPrincipal user,
        IAuthorizationStore authorizationStore,
        UserManager<ApplicationUser> userManager,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, id, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var link = await authorizationStore.FindLinkByEmployeeAsync(id, cancellationToken);
        if (link is null)
        {
            return Results.Ok(new EmployeeErpAccountSummary(false, null, null));
        }

        var account = await userManager.FindByIdAsync(link.UserId);
        var locked = account?.LockoutEnd is not null && account.LockoutEnd > DateTimeOffset.UtcNow;
        return Results.Ok(new EmployeeErpAccountSummary(true, account?.Email, locked));
    }

    private static async Task<IResult> SavePaymentProfile(
        Guid id,
        ClaimsPrincipal user,
        [FromBody] SavePaymentProfileRequest request,
        SaveEmployeePaymentProfileUseCase useCase,
        HrEmployeeCardQuery cardQuery,
        EmployeeTenantGuard tenant,
        IRequestActorContext actorContext,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, id, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var canWriteSensitive = CanReadSensitive(user);
        var actor = RequireActor(actorContext);
        var saved = await useCase.ExecuteAsync(
            new SaveEmployeePaymentProfileCommand(
                id,
                request.Iban,
                request.BankName,
                canWriteSensitive,
                actor is null ? null : ToPersonnelChangeContext(actor)),
            cancellationToken);
        if (!saved.IsSuccess)
        {
            return saved.Error!.ToHttp();
        }

        var card = await cardQuery.ExecuteAsync(id, canWriteSensitive, cancellationToken);
        return card.ToHttp();
    }

    private static bool CanReadSensitive(ClaimsPrincipal user) =>
        user.HasClaim(HrEmployeePermissions.ClaimType, HrEmployeePermissions.SensitiveRead);

    private static ActorContext? RequireActor(IRequestActorContext actorContext) => actorContext.Current;

    private static PersonnelChangeContext ToPersonnelChangeContext(ActorContext actor) =>
        new(
            actor.UserId,
            actor.EmployeeId,
            actor.OrganizationId,
            actor.PropertyId,
            actor.OccurredAtUtc,
            PersonnelChangeSources.Manual);
}

public sealed record ConfirmPersonnelImportRequest(string PreviewToken);

public sealed record SavePaymentProfileRequest(string Iban, string? BankName);

public sealed record EmployeeErpAccountSummary(bool HasAccount, string? Email, bool? IsLocked);

public sealed record PersonnelHistoryResponse(
    IReadOnlyList<PersonnelProfileChangeRecord> ProfileChanges,
    IReadOnlyList<EmploymentHistoryRecord> Employments);
