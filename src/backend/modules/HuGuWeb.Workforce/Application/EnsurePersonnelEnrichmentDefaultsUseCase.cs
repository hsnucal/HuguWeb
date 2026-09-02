using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// Idempotently seeds recruitment sources, onboarding document requirements, and HR document templates
/// for an organization. Safe for existing orgs; inserts only missing codes.
/// </summary>
public sealed class EnsurePersonnelEnrichmentDefaultsUseCase(IWorkforceStore store, IWorkforceClock clock)
{
    public const string SeedActorUserId = "system";

    public async Task<int> ExecuteAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var createdAtUtc = clock.UtcNow;
        var added = 0;

        added += await EnsureRecruitmentSourcesAsync(organizationId, createdAtUtc, cancellationToken);
        added += await EnsureOnboardingRequirementsAsync(organizationId, createdAtUtc, cancellationToken);
        added += await EnsureDocumentTemplatesAsync(organizationId, createdAtUtc, cancellationToken);

        if (added > 0)
        {
            await store.SaveChangesAsync(cancellationToken);
        }

        return added;
    }

    public async Task<int> ExecuteForAllOrganizationsAsync(CancellationToken cancellationToken)
    {
        var organizationIds = await store.ListOrganizationIdsAsync(cancellationToken);
        var added = 0;
        foreach (var organizationId in organizationIds)
        {
            added += await ExecuteAsync(organizationId, cancellationToken);
        }

        return added;
    }

    private async Task<int> EnsureRecruitmentSourcesAsync(
        Guid organizationId,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        var existing = await store.ListRecruitmentSourcesAsync(organizationId, cancellationToken);
        var present = existing
            .Select(item => RecruitmentSource.NormalizeCodeForLookup(item.Code))
            .ToHashSet(StringComparer.Ordinal);
        var added = 0;
        foreach (var definition in RecruitmentSourceDefaults.Missing(present))
        {
            store.AddRecruitmentSource(RecruitmentSource.CreateSystemDefault(
                Guid.CreateVersion7(),
                organizationId,
                definition.Code,
                definition.DefaultName,
                definition.SortOrder,
                createdAtUtc));
            added += 1;
        }

        return added;
    }

    private async Task<int> EnsureOnboardingRequirementsAsync(
        Guid organizationId,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        var existing = await store.ListOnboardingDocumentRequirementsAsync(organizationId, cancellationToken);
        var present = existing
            .Select(item => OnboardingDocumentRequirement.NormalizeCodeForLookup(item.Code))
            .ToHashSet(StringComparer.Ordinal);
        var added = 0;
        foreach (var definition in OnboardingDocumentRequirementDefaults.Missing(present))
        {
            store.AddOnboardingDocumentRequirement(OnboardingDocumentRequirement.CreateSystemDefault(
                Guid.CreateVersion7(),
                organizationId,
                definition.Code,
                definition.DefaultName,
                definition.SortOrder,
                definition.IsRequiredByDefault,
                createdAtUtc));
            added += 1;
        }

        return added;
    }

    private async Task<int> EnsureDocumentTemplatesAsync(
        Guid organizationId,
        DateTimeOffset createdAtUtc,
        CancellationToken cancellationToken)
    {
        var existing = await store.ListHrDocumentTemplatesAsync(organizationId, cancellationToken);
        var present = existing
            .Select(item => HrDocumentTemplate.NormalizeCodeForLookup(item.Code))
            .ToHashSet(StringComparer.Ordinal);
        var added = 0;
        foreach (var definition in HrDocumentTemplateDefaults.Missing(present))
        {
            store.AddHrDocumentTemplate(HrDocumentTemplate.CreateSystemDefault(
                Guid.CreateVersion7(),
                organizationId,
                definition.Code,
                definition.DefaultName,
                definition.Description,
                definition.Category,
                definition.Content,
                definition.Version,
                definition.SortOrder,
                createdAtUtc,
                definition.TemplateAssetPath));
            added += 1;
        }

        return added;
    }
}
