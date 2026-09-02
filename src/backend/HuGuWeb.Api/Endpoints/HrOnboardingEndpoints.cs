using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Context;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using Microsoft.AspNetCore.Mvc;

namespace HuGuWeb.Api.Endpoints;

public static class HrOnboardingEndpoints
{
    public static IEndpointRouteBuilder MapHrOnboardingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/hr")
            .WithTags("HR Personnel Enrichment")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeRead);

        group.MapGet("/onboarding/catalog", GetOnboardingCatalog)
            .WithName("GetOnboardingCatalog");
        group.MapGet("/onboarding-document-requirements", ListOnboardingDocumentRequirements)
            .WithName("ListOnboardingDocumentRequirements");
        group.MapGet("/employees/{id:guid}/onboarding-documents", GetOnboardingChecklist)
            .WithName("GetHrEmployeeOnboardingDocuments");
        group.MapPut("/employees/{id:guid}/onboarding-documents/{requirementId:guid}", SetOnboardingChecklistItem)
            .WithName("SetHrEmployeeOnboardingDocument")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/employees/{id:guid}/onboarding-documents/sync", SyncOnboardingChecklist)
            .WithName("SyncHrEmployeeOnboardingDocuments")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/employees/{id:guid}/onboarding-documents/complete", CompleteOnboarding)
            .WithName("CompleteHrEmployeeOnboarding")
            .RequireAuthorization(AuthorizationPolicies.HrEmployeeManage)
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapGet("/document-templates", ListDocumentTemplates)
            .WithName("ListHrDocumentTemplates");
        group.MapGet("/employees/{id:guid}/document-templates/{templateId:guid}/preview", PreviewDocumentTemplate)
            .WithName("PreviewHrEmployeeDocumentTemplate");
        group.MapGet("/employees/{id:guid}/document-templates/{templateId:guid}/docx", DownloadDocumentDocx)
            .WithName("DownloadHrEmployeeDocumentDocx");
        group.MapGet("/recruitment-sources", ListRecruitmentSources)
            .WithName("ListHrRecruitmentSources");
        group.MapPost("/onboarding/document-templates/{templateId:guid}/preview-draft", PreviewDocumentTemplateDraft)
            .WithName("PreviewHrOnboardingDocumentTemplateDraft")
            .AddEndpointFilter<ValidateAntiforgeryFilter>();
        group.MapPost("/onboarding/document-templates/{templateId:guid}/generate-draft", GenerateDocumentTemplateDraft)
            .WithName("GenerateHrOnboardingDocumentTemplateDraft")
            .AddEndpointFilter<ValidateAntiforgeryFilter>();

        return endpoints;
    }

    private static async Task<IResult> GetOnboardingCatalog(
        OnboardingCatalogQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ListOnboardingDocumentRequirements(
        ListOnboardingDocumentRequirementsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GetOnboardingChecklist(
        Guid id,
        ClaimsPrincipal user,
        OnboardingChecklistQuery query,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, id, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var result = await query.ExecuteAsync(id, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> SetOnboardingChecklistItem(
        Guid id,
        Guid requirementId,
        ClaimsPrincipal user,
        [FromBody] SetOnboardingDocumentRequest request,
        SetOnboardingChecklistItemUseCase useCase,
        EmployeeTenantGuard tenant,
        IRequestActorContext actorContext,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, id, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var actor = actorContext.Current;
        if (actor is null)
        {
            return Results.Unauthorized();
        }

        var result = await useCase.ExecuteAsync(
            new SetOnboardingChecklistItemCommand(
                id,
                requirementId,
                request.IsCompleted,
                actor.UserId),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> SyncOnboardingChecklist(
        Guid id,
        ClaimsPrincipal user,
        [FromBody] SyncOnboardingDocumentsRequest request,
        SyncOnboardingChecklistUseCase useCase,
        EmployeeTenantGuard tenant,
        IRequestActorContext actorContext,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, id, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var actor = actorContext.Current;
        if (actor is null)
        {
            return Results.Unauthorized();
        }

        var result = await useCase.ExecuteAsync(
            new SyncOnboardingChecklistCommand(
                id,
                request.CompletedRequirementIds ?? [],
                actor.UserId),
            cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> CompleteOnboarding(
        Guid id,
        ClaimsPrincipal user,
        CompleteEmploymentOnboardingUseCase useCase,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, id, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var result = await useCase.ExecuteAsync(id, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> ListDocumentTemplates(
        [FromQuery] string? category,
        HrDocumentTemplateQuery query,
        CancellationToken cancellationToken)
    {
        HrDocumentTemplateCategory? parsed = null;
        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!Enum.TryParse<HrDocumentTemplateCategory>(category, ignoreCase: true, out var value))
            {
                return WorkforceError.InvalidRequest("invalid-category", "Document template category is invalid.")
                    .ToHttp();
            }

            parsed = value;
        }

        var result = await query.ListActiveAsync(parsed, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> PreviewDocumentTemplate(
        Guid id,
        Guid templateId,
        ClaimsPrincipal user,
        PreviewHrDocumentTemplateUseCase useCase,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, id, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var result = await useCase.ExecuteAsync(id, templateId, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> DownloadDocumentDocx(
        Guid id,
        Guid templateId,
        ClaimsPrincipal user,
        RenderHrDocumentDocxUseCase useCase,
        EmployeeTenantGuard tenant,
        CancellationToken cancellationToken)
    {
        if (!await tenant.AllowsEmployeeAsync(user, id, cancellationToken))
        {
            return WorkforceError.EmployeeNotFound().ToHttp();
        }

        var result = await useCase.ExecuteAsync(id, templateId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!.ToHttp();
        }

        var file = result.Value;
        return Results.File(
            file.Content,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            file.FileName);
    }

    private static async Task<IResult> ListRecruitmentSources(
        ListRecruitmentSourcesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await query.ExecuteAsync(activeOnly: true, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> PreviewDocumentTemplateDraft(
        Guid templateId,
        [FromBody] HrOnboardingDocumentDraftRequest request,
        PreviewHrDocumentTemplateDraftUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(templateId, request, cancellationToken);
        return result.ToHttp();
    }

    private static async Task<IResult> GenerateDocumentTemplateDraft(
        Guid templateId,
        [FromBody] HrOnboardingDocumentDraftRequest request,
        RenderHrDocumentDraftDocxUseCase useCase,
        CancellationToken cancellationToken)
    {
        var result = await useCase.ExecuteAsync(templateId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!.ToHttp();
        }

        var file = result.Value;
        return Results.File(
            file!.Content,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            file.FileName);
    }
}

public sealed record SetOnboardingDocumentRequest(bool IsCompleted);

public sealed record SyncOnboardingDocumentsRequest(IReadOnlyList<Guid>? CompletedRequirementIds);
