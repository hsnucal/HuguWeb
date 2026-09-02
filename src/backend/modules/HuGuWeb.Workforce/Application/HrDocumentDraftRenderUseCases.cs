using System.Globalization;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

internal static class HrDocumentTemplateResolver
{
    public static async Task<WorkforceResult<HrDocumentTemplate>> ResolveActiveAsync(
        IWorkforceStore store,
        Guid organizationId,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var template = await store.GetHrDocumentTemplateAsync(templateId, cancellationToken);
        if (template is null
            || template.OrganizationId != organizationId
            || !template.IsActive)
        {
            return WorkforceError.NotFound(
                HrValidation.Codes.DocumentTemplateNotFound,
                "HR document template was not found.") with
            {
                Errors = new Dictionary<string, string[]>
                {
                    [HrValidation.Fields.DocumentTemplateId] = [HrValidation.Codes.DocumentTemplateNotFound]
                }
            };
        }

        return template;
    }
}

internal static class HrDocumentDraftRendering
{
    public static WorkforceResult<HrOnboardingDocumentDraft> ValidateDraft(
        HrOnboardingDocumentDraftRequest request,
        HrDocumentTemplate template)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var givenName = request.GivenName?.Trim() ?? string.Empty;
        var familyName = request.FamilyName?.Trim() ?? string.Empty;

        if (givenName.Length == 0)
        {
            errors[HrValidation.Fields.GivenName] = [HrValidation.Codes.GivenNameRequired];
        }
        else if (givenName.Length > Employee.NameMaxLength)
        {
            errors[HrValidation.Fields.GivenName] = [HrValidation.Codes.GivenNameTooLong];
        }

        if (familyName.Length == 0)
        {
            errors[HrValidation.Fields.FamilyName] = [HrValidation.Codes.FamilyNameRequired];
        }
        else if (familyName.Length > Employee.NameMaxLength)
        {
            errors[HrValidation.Fields.FamilyName] = [HrValidation.Codes.FamilyNameTooLong];
        }

        if (string.IsNullOrWhiteSpace(request.EmploymentStartDate))
        {
            errors[HrValidation.Fields.EmploymentStartDate] = [HrValidation.Codes.StartDateRequired];
        }
        else if (!DateOnly.TryParse(
                     request.EmploymentStartDate,
                     CultureInfo.InvariantCulture,
                     DateTimeStyles.None,
                     out var startDate))
        {
            errors[HrValidation.Fields.EmploymentStartDate] = [HrValidation.Codes.StartDateRequired];
        }
        else
        {
            foreach (var placeholder in HrDocumentPlaceholderCatalog.PlaceholdersInContent(template.Content))
            {
                if (placeholder == HrDocumentPlaceholderCatalog.EmploymentStartDate && startDate == default)
                {
                    errors[HrValidation.Fields.EmploymentStartDate] = [HrValidation.Codes.StartDateRequired];
                }
            }

            if (errors.Count == 0)
            {
                return HrOnboardingDocumentDraft.Create(
                    givenName,
                    familyName,
                    startDate,
                    request.DepartmentName?.Trim(),
                    request.PositionName?.Trim());
            }
        }

        return WorkforceError.InvalidFields(
            "invalid-request",
            "Draft personnel data is incomplete for this template.",
            errors);
    }

    public static HrDocumentRenderContext BuildRenderContext(
        HrOnboardingDocumentDraft draft,
        ConfiguredOrganization workplace,
        string? propertyName,
        DateOnly today) =>
        new(
            draft.FullName,
            draft.GivenName,
            draft.FamilyName,
            string.Empty,
            null,
            draft.EmploymentStartDate,
            draft.DepartmentName ?? string.Empty,
            draft.PositionName ?? string.Empty,
            workplace.Organization.Name,
            propertyName ?? string.Empty,
            today);

    public static Dictionary<string, string> BuildDocxPlaceholderValues(HrDocumentRenderContext context) =>
        new(StringComparer.Ordinal)
        {
            [HrDocumentPlaceholderCatalog.EmployeeFullName] = context.EmployeeFullName,
            [HrDocumentPlaceholderCatalog.EmploymentStartDate] =
                HrDocumentDocxRenderer.FormatTrDate(context.EmploymentStartDate),
        };

    public static WorkforceResult<string> RenderPreviewContent(
        HrDocumentTemplate template,
        HrDocumentRenderContext context,
        CultureInfo culture)
    {
        if (!string.IsNullOrWhiteSpace(template.TemplateAssetPath))
        {
            try
            {
                var values = BuildDocxPlaceholderValues(context);
                return WorkforceResult<string>.Success(
                    HrDocumentDocxRenderer.BuildPreviewHtml(template.TemplateAssetPath, values));
            }
            catch (FileNotFoundException)
            {
                return WorkforceError.Conflict(
                    HrValidation.Codes.DocumentTemplateAssetMissing,
                    "DOCX asset missing",
                    "DOCX template asset is missing from the application.");
            }
            catch (Exception)
            {
                return WorkforceError.Conflict(
                    HrValidation.Codes.DocumentTemplateDocxUnavailable,
                    "DOCX unavailable",
                    "DOCX template could not be rendered.");
            }
        }

        if (!HrDocumentTemplateRenderer.TryRender(
                template.Content,
                context,
                culture,
                out var rendered,
                out var field,
                out var code))
        {
            return WorkforceError.InvalidFields(
                code ?? HrValidation.Codes.DocumentTemplateUnknownPlaceholder,
                "Document template content is invalid.",
                field ?? HrValidation.Fields.DocumentTemplateContent,
                code ?? HrValidation.Codes.DocumentTemplateUnknownPlaceholder);
        }

        return WorkforceResult<string>.Success(rendered);
    }
}

public sealed class PreviewHrDocumentTemplateDraftUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<HrDocumentTemplatePreview>> ExecuteAsync(
        Guid templateId,
        HrOnboardingDocumentDraftRequest request,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var templateResult = await HrDocumentTemplateResolver.ResolveActiveAsync(
            store,
            workplace.Value.Organization.Id,
            templateId,
            cancellationToken);
        if (!templateResult.IsSuccess)
        {
            return templateResult.Error!;
        }

        var template = templateResult.Value!;
        var draftResult = HrDocumentDraftRendering.ValidateDraft(request, template);
        if (!draftResult.IsSuccess)
        {
            return draftResult.Error!;
        }

        var propertyName = string.Empty;
        if (workplaceContext.HasProperty)
        {
            var property = await store.GetPropertyAsync(workplaceContext.PropertyId, cancellationToken);
            propertyName = property?.Name ?? string.Empty;
        }

        var context = HrDocumentDraftRendering.BuildRenderContext(
            draftResult.Value!,
            workplace.Value,
            propertyName,
            clock.Today);

        var renderedResult = HrDocumentDraftRendering.RenderPreviewContent(
            template,
            context,
            CultureInfo.GetCultureInfo("tr-TR"));
        if (!renderedResult.IsSuccess)
        {
            return renderedResult.Error!;
        }

        return new HrDocumentTemplatePreview(
            template.Id,
            template.Code,
            template.Name,
            template.Version,
            renderedResult.Value!);
    }
}

public sealed class RenderHrDocumentDraftDocxUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<HrDocumentDocxFile>> ExecuteAsync(
        Guid templateId,
        HrOnboardingDocumentDraftRequest request,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var templateResult = await HrDocumentTemplateResolver.ResolveActiveAsync(
            store,
            workplace.Value.Organization.Id,
            templateId,
            cancellationToken);
        if (!templateResult.IsSuccess)
        {
            return templateResult.Error!;
        }

        var template = templateResult.Value!;
        if (string.IsNullOrWhiteSpace(template.TemplateAssetPath))
        {
            return WorkforceError.Conflict(
                HrValidation.Codes.DocumentTemplateDocxUnavailable,
                "DOCX unavailable",
                "This template does not have a DOCX asset.");
        }

        var draftResult = HrDocumentDraftRendering.ValidateDraft(request, template);
        if (!draftResult.IsSuccess)
        {
            return draftResult.Error!;
        }

        var context = HrDocumentDraftRendering.BuildRenderContext(
            draftResult.Value!,
            workplace.Value,
            propertyName: null,
            clock.Today);
        var values = HrDocumentDraftRendering.BuildDocxPlaceholderValues(context);

        byte[] bytes;
        try
        {
            bytes = HrDocumentDocxRenderer.Render(template.TemplateAssetPath, values);
        }
        catch (FileNotFoundException)
        {
            return WorkforceError.Conflict(
                HrValidation.Codes.DocumentTemplateAssetMissing,
                "DOCX asset missing",
                "DOCX template asset is missing from the application.");
        }
        catch (Exception)
        {
            return WorkforceError.Conflict(
                HrValidation.Codes.DocumentTemplateDocxUnavailable,
                "DOCX unavailable",
                "DOCX template could not be rendered.");
        }

        var fileName =
            $"{HrDocumentDocxRenderer.SanitizeFileName(template.Name)}_{HrDocumentDocxRenderer.SanitizeFileName(context.EmployeeFullName)}.docx";
        return new HrDocumentDocxFile(fileName, bytes);
    }
}

public sealed record HrOnboardingDocumentDraftRequest(
    string GivenName,
    string FamilyName,
    string EmploymentStartDate,
    string? DepartmentName,
    string? PositionName);

public sealed record HrOnboardingDocumentDraft(
    string GivenName,
    string FamilyName,
    string FullName,
    DateOnly EmploymentStartDate,
    string? DepartmentName,
    string? PositionName)
{
    public static WorkforceResult<HrOnboardingDocumentDraft> Create(
        string givenName,
        string familyName,
        DateOnly employmentStartDate,
        string? departmentName,
        string? positionName) =>
        WorkforceResult<HrOnboardingDocumentDraft>.Success(
            new HrOnboardingDocumentDraft(
                givenName,
                familyName,
                $"{givenName} {familyName}".Trim(),
                employmentStartDate,
                departmentName,
                positionName));
}
