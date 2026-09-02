using System.Globalization;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class HrDocumentTemplateQuery(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<IReadOnlyList<HrDocumentTemplateListItem>>> ListActiveAsync(
        HrDocumentTemplateCategory? category,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var templates = await store.ListHrDocumentTemplatesAsync(
            workplace.Value.Organization.Id,
            cancellationToken);
        var filtered = templates
            .Where(item => item.IsActive)
            .Where(item => category is null || item.Category == category)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .Select(item => new HrDocumentTemplateListItem(
                item.Id,
                item.Code,
                item.Name,
                item.Description,
                item.Category,
                item.Version,
                item.SortOrder,
                !string.IsNullOrWhiteSpace(item.TemplateAssetPath)))
            .ToArray();
        return WorkforceResult<IReadOnlyList<HrDocumentTemplateListItem>>.Success(filtered);
    }
}

public sealed class PreviewHrDocumentTemplateUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<HrDocumentTemplatePreview>> ExecuteAsync(
        Guid employeeId,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var template = await store.GetHrDocumentTemplateAsync(templateId, cancellationToken);
        if (template is null
            || template.OrganizationId != workplace.Value.Organization.Id
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

        var employments = await store.ListEmploymentsAsync(employeeId, cancellationToken);
        var employment = OfficialEmploymentSelection.ForEmployee(employments);
        if (!employment.IsSuccess)
        {
            return employment.Error!;
        }

        if (!employment.Value.IsOnboardingMutable)
        {
            return WorkforceError.Conflict(
                HrValidation.Codes.OnboardingDocumentGenerationClosed,
                "Onboarding document generation closed",
                "Matbu evrak önizleme yalnızca işe giriş sürecinde kullanılabilir.") with
            {
                Errors = new Dictionary<string, string[]>
                {
                    [HrValidation.Fields.OnboardingIsCompleted] =
                        [HrValidation.Codes.OnboardingDocumentGenerationClosed]
                }
            };
        }

        var profile = await store.GetHrProfileAsync(employeeId, cancellationToken);
        var assignments = await store.ListAssignmentsAsync(employment.Value.Id, cancellationToken);
        var primary = assignments
            .Where(item => item.Kind == AssignmentKind.Primary)
            .OrderByDescending(item => item.StartDate)
            .FirstOrDefault();
        var departmentName = string.Empty;
        var positionName = string.Empty;
        var propertyName = string.Empty;
        if (primary is not null)
        {
            var department = await store.GetDepartmentAsync(primary.DepartmentId, cancellationToken);
            var position = await store.GetPositionAsync(primary.PositionId, cancellationToken);
            departmentName = department?.Name ?? string.Empty;
            positionName = position?.Name ?? string.Empty;
            if (department is not null)
            {
                var property = await store.GetPropertyAsync(department.PropertyId, cancellationToken);
                propertyName = property?.Name ?? string.Empty;
            }
        }

        if (workplaceContext.HasProperty)
        {
            var selected = await store.GetPropertyAsync(workplaceContext.PropertyId, cancellationToken);
            if (selected is not null)
            {
                propertyName = selected.Name;
            }
        }

        var context = new HrDocumentRenderContext(
            $"{employee.GivenName} {employee.FamilyName}".Trim(),
            employee.GivenName,
            employee.FamilyName,
            employee.PersonnelNumber,
            profile?.BirthDate,
            employment.Value.StartDate,
            departmentName,
            positionName,
            workplace.Value.Organization.Name,
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

public sealed class ListRecruitmentSourcesQuery(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<IReadOnlyList<RecruitmentSourceListItem>>> ExecuteAsync(
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var sources = await store.ListRecruitmentSourcesAsync(
            workplace.Value.Organization.Id,
            cancellationToken);
        var filtered = sources
            .Where(item => !activeOnly || item.IsActive)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .Select(item => new RecruitmentSourceListItem(item.Id, item.Code, item.Name, item.IsActive, item.SortOrder))
            .ToArray();
        return WorkforceResult<IReadOnlyList<RecruitmentSourceListItem>>.Success(filtered);
    }
}

public sealed record HrDocumentTemplateListItem(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    HrDocumentTemplateCategory Category,
    string Version,
    int SortOrder,
    bool HasDocxAsset);

public sealed record HrDocumentTemplatePreview(
    Guid TemplateId,
    string Code,
    string Name,
    string Version,
    string RenderedContent);

public sealed record RecruitmentSourceListItem(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    int SortOrder);
