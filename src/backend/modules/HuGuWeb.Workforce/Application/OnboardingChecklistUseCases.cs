using System.Globalization;
using System.Text;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

internal static class OnboardingCatalogResolver
{
    public static async Task<OnboardingCatalogReadModel> LoadAsync(
        IWorkforceStore store,
        EnsurePersonnelEnrichmentDefaultsUseCase ensureDefaults,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var catalog = await ReadCatalogAsync(store, organizationId, cancellationToken);
        if (catalog.Requirements.Count == 0 && catalog.DocumentTemplates.Count == 0)
        {
            await ensureDefaults.ExecuteAsync(organizationId, cancellationToken);
            catalog = await ReadCatalogAsync(store, organizationId, cancellationToken);
        }

        return catalog;
    }

    private static async Task<OnboardingCatalogReadModel> ReadCatalogAsync(
        IWorkforceStore store,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var requirements = (await store.ListOnboardingDocumentRequirementsAsync(organizationId, cancellationToken))
            .Where(item => item.IsActive)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .Select(item => new OnboardingRequirementCatalogItem(
                item.Id,
                item.Code,
                item.Name,
                item.IsRequiredByDefault))
            .ToArray();

        var templates = (await store.ListHrDocumentTemplatesAsync(organizationId, cancellationToken))
            .Where(item => item.IsActive && item.Category == HrDocumentTemplateCategory.Onboarding)
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

        return new OnboardingCatalogReadModel(requirements, templates);
    }
}

public sealed class OnboardingCatalogQuery(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext,
    EnsurePersonnelEnrichmentDefaultsUseCase ensureDefaults)
{
    public async Task<WorkforceResult<OnboardingCatalogReadModel>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var catalog = await OnboardingCatalogResolver.LoadAsync(
            store,
            ensureDefaults,
            workplace.Value.Organization.Id,
            cancellationToken);
        return WorkforceResult<OnboardingCatalogReadModel>.Success(catalog);
    }
}

public sealed class OnboardingChecklistQuery(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext,
    EnsurePersonnelEnrichmentDefaultsUseCase ensureDefaults)
{
    public async Task<WorkforceResult<OnboardingChecklistReadModel>> ExecuteAsync(
        Guid employeeId,
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

        var employments = await store.ListEmploymentsAsync(employeeId, cancellationToken);
        var employment = OfficialEmploymentSelection.ForEmployee(employments);
        if (!employment.IsSuccess)
        {
            return employment.Error!;
        }

        var catalog = await OnboardingCatalogResolver.LoadAsync(
            store,
            ensureDefaults,
            workplace.Value.Organization.Id,
            cancellationToken);

        var statuses = await store.ListEmploymentOnboardingDocumentStatusesAsync(
            employment.Value.Id,
            cancellationToken);
        var byRequirement = statuses.ToDictionary(item => item.RequirementId);

        var items = catalog.Requirements.Select(requirement =>
        {
            byRequirement.TryGetValue(requirement.Id, out var status);
            return new OnboardingChecklistItemReadModel(
                requirement.Id,
                requirement.Code,
                requirement.Name,
                requirement.IsRequiredByDefault,
                status?.IsCompleted ?? false,
                status?.CompletedAtUtc,
                status?.CompletedByUserId);
        }).ToArray();

        var completed = items.Count(item => item.IsCompleted);
        var canEdit = employment.Value.IsOnboardingMutable;
        return new OnboardingChecklistReadModel(
            employment.Value.Id,
            employment.Value.OnboardingStatus.ToString(),
            canEdit,
            canEdit,
            items,
            items.Length,
            completed,
            catalog.DocumentTemplates);
    }
}

public sealed class ListOnboardingDocumentRequirementsQuery(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext,
    EnsurePersonnelEnrichmentDefaultsUseCase ensureDefaults)
{
    public async Task<WorkforceResult<IReadOnlyList<OnboardingRequirementCatalogItem>>> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var catalog = await new OnboardingCatalogQuery(store, workplaceContext, ensureDefaults)
            .ExecuteAsync(cancellationToken);
        if (!catalog.IsSuccess)
        {
            return catalog.Error!;
        }

        return WorkforceResult<IReadOnlyList<OnboardingRequirementCatalogItem>>.Success(catalog.Value!.Requirements);
    }
}

public sealed class SetOnboardingChecklistItemUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<OnboardingChecklistItemReadModel>> ExecuteAsync(
        SetOnboardingChecklistItemCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var employee = await store.GetEmployeeAsync(command.EmployeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var employments = await store.ListEmploymentsAsync(command.EmployeeId, cancellationToken);
        var employment = OfficialEmploymentSelection.ForEmployee(employments);
        if (!employment.IsSuccess)
        {
            return employment.Error!;
        }

        if (!employment.Value.IsOnboardingMutable)
        {
            return WorkforceError.Conflict(
                HrValidation.Codes.OnboardingDocumentsReadOnly,
                "Onboarding documents read-only",
                "Onboarding checklist is read-only because onboarding is completed.") with
            {
                Errors = new Dictionary<string, string[]>
                {
                    [HrValidation.Fields.OnboardingIsCompleted] =
                        [HrValidation.Codes.OnboardingDocumentsReadOnly]
                }
            };
        }

        var requirement = await store.GetOnboardingDocumentRequirementAsync(
            command.RequirementId,
            cancellationToken);
        if (requirement is null
            || requirement.OrganizationId != workplace.Value.Organization.Id
            || !requirement.IsActive)
        {
            return WorkforceError.NotFound(
                HrValidation.Codes.OnboardingRequirementNotFound,
                "Onboarding document requirement was not found.") with
            {
                Errors = new Dictionary<string, string[]>
                {
                    [HrValidation.Fields.OnboardingRequirementId] =
                        [HrValidation.Codes.OnboardingRequirementNotFound]
                }
            };
        }

        var statuses = await store.ListEmploymentOnboardingDocumentStatusesAsync(
            employment.Value.Id,
            cancellationToken);
        var status = statuses.FirstOrDefault(item => item.RequirementId == requirement.Id);
        var utcNow = clock.UtcNow;
        if (status is null)
        {
            status = EmploymentOnboardingDocumentStatus.Create(
                Guid.CreateVersion7(),
                employment.Value.Id,
                requirement.Id,
                utcNow);
            store.AddEmploymentOnboardingDocumentStatus(status);
        }

        if (command.IsCompleted)
        {
            status.MarkCompleted(command.ActorUserId, utcNow);
        }
        else
        {
            status.MarkIncomplete(utcNow);
        }

        await store.SaveChangesAsync(cancellationToken);

        return new OnboardingChecklistItemReadModel(
            requirement.Id,
            requirement.Code,
            requirement.Name,
            requirement.IsRequiredByDefault,
            status.IsCompleted,
            status.CompletedAtUtc,
            status.CompletedByUserId);
    }
}

public sealed class CompleteEmploymentOnboardingUseCase(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext,
    OnboardingChecklistQuery checklistQuery)
{
    public async Task<WorkforceResult<OnboardingChecklistReadModel>> ExecuteAsync(
        Guid employeeId,
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

        var employments = await store.ListEmploymentsAsync(employeeId, cancellationToken);
        var employment = OfficialEmploymentSelection.ForEmployee(employments);
        if (!employment.IsSuccess)
        {
            return employment.Error!;
        }

        if (!employment.Value.TryCompleteOnboarding(out _))
        {
            return WorkforceError.Conflict(
                HrValidation.Codes.OnboardingAlreadyCompleted,
                "Onboarding already completed",
                "Onboarding is already completed.") with
            {
                Errors = new Dictionary<string, string[]>
                {
                    [HrValidation.Fields.OnboardingIsCompleted] =
                        [HrValidation.Codes.OnboardingAlreadyCompleted]
                }
            };
        }

        await store.SaveChangesAsync(cancellationToken);
        return await checklistQuery.ExecuteAsync(employeeId, cancellationToken);
    }
}

public sealed class RenderHrDocumentDocxUseCase(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<HrDocumentDocxFile>> ExecuteAsync(
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
                "HR document template was not found.");
        }

        if (string.IsNullOrWhiteSpace(template.TemplateAssetPath))
        {
            return WorkforceError.Conflict(
                HrValidation.Codes.DocumentTemplateDocxUnavailable,
                "DOCX unavailable",
                "This template does not have a DOCX asset.");
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
                "Matbu evrak oluşturma yalnızca işe giriş sürecinde kullanılabilir.") with
            {
                Errors = new Dictionary<string, string[]>
                {
                    [HrValidation.Fields.OnboardingIsCompleted] =
                        [HrValidation.Codes.OnboardingDocumentGenerationClosed]
                }
            };
        }

        var fullName = $"{employee.GivenName} {employee.FamilyName}".Trim();
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{{Employee.FullName}}"] = fullName,
            ["{{Employment.StartDate}}"] = HrDocumentDocxRenderer.FormatTrDate(employment.Value.StartDate),
        };

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
            $"{HrDocumentDocxRenderer.SanitizeFileName(template.Name)}_{HrDocumentDocxRenderer.SanitizeFileName(fullName)}.docx";
        return new HrDocumentDocxFile(fileName, bytes);
    }
}

public sealed record SetOnboardingChecklistItemCommand(
    Guid EmployeeId,
    Guid RequirementId,
    bool IsCompleted,
    string ActorUserId);

public sealed class SyncOnboardingChecklistUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext,
    OnboardingChecklistQuery checklistQuery)
{
    public async Task<WorkforceResult<OnboardingChecklistReadModel>> ExecuteAsync(
        SyncOnboardingChecklistCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var employee = await store.GetEmployeeAsync(command.EmployeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var employments = await store.ListEmploymentsAsync(command.EmployeeId, cancellationToken);
        var employment = OfficialEmploymentSelection.ForEmployee(employments);
        if (!employment.IsSuccess)
        {
            return employment.Error!;
        }

        if (!employment.Value.IsOnboardingMutable)
        {
            return WorkforceError.Conflict(
                HrValidation.Codes.OnboardingDocumentsReadOnly,
                "Onboarding documents read-only",
                "Onboarding checklist is read-only because onboarding is completed.") with
            {
                Errors = new Dictionary<string, string[]>
                {
                    [HrValidation.Fields.OnboardingIsCompleted] =
                        [HrValidation.Codes.OnboardingDocumentsReadOnly]
                }
            };
        }

        var requirements = (await store.ListOnboardingDocumentRequirementsAsync(
                workplace.Value.Organization.Id,
                cancellationToken))
            .Where(item => item.IsActive)
            .ToDictionary(item => item.Id);
        var statuses = await store.ListEmploymentOnboardingDocumentStatusesAsync(
            employment.Value.Id,
            cancellationToken);
        var byRequirement = statuses.ToDictionary(item => item.RequirementId);
        var utcNow = clock.UtcNow;
        var completedIds = command.CompletedRequirementIds.ToHashSet();

        foreach (var requirement in requirements.Values)
        {
            byRequirement.TryGetValue(requirement.Id, out var status);
            var shouldComplete = completedIds.Contains(requirement.Id);
            if (shouldComplete)
            {
                if (status is null)
                {
                    status = EmploymentOnboardingDocumentStatus.Create(
                        Guid.CreateVersion7(),
                        employment.Value.Id,
                        requirement.Id,
                        utcNow);
                    store.AddEmploymentOnboardingDocumentStatus(status);
                    byRequirement[requirement.Id] = status;
                }

                status.MarkCompleted(command.ActorUserId, utcNow);
            }
            else if (status is not null)
            {
                status.MarkIncomplete(utcNow);
            }
        }

        await store.SaveChangesAsync(cancellationToken);
        return await checklistQuery.ExecuteAsync(command.EmployeeId, cancellationToken);
    }
}

public sealed record SyncOnboardingChecklistCommand(
    Guid EmployeeId,
    IReadOnlyCollection<Guid> CompletedRequirementIds,
    string ActorUserId);

public sealed record OnboardingChecklistReadModel(
    Guid EmploymentId,
    string OnboardingStatus,
    bool CanEditChecklist,
    bool CanGenerateDocuments,
    IReadOnlyList<OnboardingChecklistItemReadModel> Items,
    int TotalCount,
    int CompletedCount,
    IReadOnlyList<HrDocumentTemplateListItem> DocumentTemplates);

public sealed record OnboardingCatalogReadModel(
    IReadOnlyList<OnboardingRequirementCatalogItem> Requirements,
    IReadOnlyList<HrDocumentTemplateListItem> DocumentTemplates);

public sealed record OnboardingRequirementCatalogItem(
    Guid Id,
    string Code,
    string Name,
    bool IsRequiredByDefault);

public sealed record OnboardingChecklistItemReadModel(
    Guid RequirementId,
    string Code,
    string Name,
    bool IsRequiredByDefault,
    bool IsCompleted,
    DateTimeOffset? CompletedAtUtc,
    string? CompletedByUserId);

public sealed record HrDocumentDocxFile(string FileName, byte[] Content);
