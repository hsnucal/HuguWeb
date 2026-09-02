using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public sealed class PersonnelFinding05OnboardingCatalogTests
{
    [Fact]
    public async Task Catalog_ReturnsSevenRequirements_AndMatbuTemplate_WithoutEmployeeOrEmployment()
    {
        var harness = new WorkforceHarness();

        var catalog = await harness.OnboardingCatalog.ExecuteAsync(CancellationToken.None);

        Assert.True(catalog.IsSuccess, catalog.Error?.Detail);
        Assert.Equal(7, catalog.Value!.Requirements.Count);
        Assert.Contains(catalog.Value.Requirements, item => item.Code == "ID_COPY");
        Assert.Contains(catalog.Value.Requirements, item => item.Code == "BANK_IBAN");
        Assert.Single(catalog.Value.DocumentTemplates);
        Assert.Equal("OVERTIME-CONSENT", catalog.Value.DocumentTemplates[0].Code);
        Assert.True(catalog.Value.DocumentTemplates[0].HasDocxAsset);
    }

    [Fact]
    public async Task Catalog_SeedsDefaults_WhenOrganizationCatalogEmpty()
    {
        var harness = new WorkforceHarness();
        Assert.Empty(harness.Store.OnboardingDocumentRequirements);
        Assert.Empty(harness.Store.HrDocumentTemplates);

        var catalog = await harness.OnboardingCatalog.ExecuteAsync(CancellationToken.None);

        Assert.True(catalog.IsSuccess, catalog.Error?.Detail);
        Assert.Equal(7, catalog.Value!.Requirements.Count);
        Assert.Single(catalog.Value.DocumentTemplates);
        Assert.Equal(7, harness.Store.OnboardingDocumentRequirements.Count);
        Assert.Single(harness.Store.HrDocumentTemplates);
    }

    [Fact]
    public async Task Catalog_ExcludesOtherOrganizationDefinitions()
    {
        var harness = new WorkforceHarness();
        var otherOrgId = Guid.CreateVersion7();
        harness.Store.Organizations.Add(new Organization(otherOrgId, "Other Organization"));

        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(otherOrgId, CancellationToken.None);

        var otherOnly = OnboardingDocumentRequirement.CreateSystemDefault(
            Guid.CreateVersion7(),
            otherOrgId,
            "OTHER-ONLY",
            "Other Org Requirement",
            sortOrder: 99,
            isRequiredByDefault: true,
            DateTimeOffset.UtcNow);
        harness.Store.AddOnboardingDocumentRequirement(otherOnly);

        var catalog = await harness.OnboardingCatalog.ExecuteAsync(CancellationToken.None);

        Assert.True(catalog.IsSuccess);
        Assert.Equal(7, catalog.Value!.Requirements.Count);
        Assert.DoesNotContain(catalog.Value.Requirements, item => item.Code == "OTHER-ONLY");
    }

    [Fact]
    public async Task ChecklistGet_DoesNotCreateEmploymentStatusRows()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);

        Assert.Empty(harness.Store.EmploymentOnboardingDocumentStatuses);

        var checklist = await harness.OnboardingChecklist.ExecuteAsync(
            hired.Value!.EmployeeId,
            CancellationToken.None);
        Assert.True(checklist.IsSuccess, checklist.Error?.Detail);
        Assert.Equal(7, checklist.Value!.TotalCount);
        Assert.Equal(0, checklist.Value.CompletedCount);
        Assert.Empty(harness.Store.EmploymentOnboardingDocumentStatuses);
    }

    [Fact]
    public async Task CompletedEmployment_ExposesCatalogTemplates_ReadOnly_WithPartialChecklist()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var idCopy = harness.Store.OnboardingDocumentRequirements.Single(item => item.Code == "ID_COPY");
        var criminal = harness.Store.OnboardingDocumentRequirements.Single(item => item.Code == "CRIMINAL_RECORD");

        Assert.True((await harness.SetOnboardingChecklistItem.ExecuteAsync(
            new SetOnboardingChecklistItemCommand(hired.Value!.EmployeeId, idCopy.Id, true, "actor"),
            CancellationToken.None)).IsSuccess);
        Assert.True((await harness.SetOnboardingChecklistItem.ExecuteAsync(
            new SetOnboardingChecklistItemCommand(hired.Value.EmployeeId, criminal.Id, true, "actor"),
            CancellationToken.None)).IsSuccess);

        Assert.True((await harness.CompleteEmploymentOnboarding.ExecuteAsync(
            hired.Value.EmployeeId,
            CancellationToken.None)).IsSuccess);

        var historical = await harness.OnboardingChecklist.ExecuteAsync(
            hired.Value.EmployeeId,
            CancellationToken.None);
        Assert.True(historical.IsSuccess);
        Assert.Equal(7, historical.Value!.Items.Count);
        Assert.Single(historical.Value.DocumentTemplates);
        Assert.Equal("OVERTIME-CONSENT", historical.Value.DocumentTemplates[0].Code);
        Assert.Equal(2, historical.Value.CompletedCount);
        Assert.False(historical.Value.CanEditChecklist);
        Assert.False(historical.Value.CanGenerateDocuments);

        var items = historical.Value.Items.ToDictionary(item => item.Code);
        Assert.True(items["ID_COPY"].IsCompleted);
        Assert.True(items["CRIMINAL_RECORD"].IsCompleted);
        Assert.False(items["DIPLOMA"].IsCompleted);
        Assert.False(items["RESIDENCE"].IsCompleted);
    }

    [Fact]
    public async Task Catalog_RequiresOrganizationContext_NoSilentFallback()
    {
        var store = new InMemoryWorkforceStore();
        var workplace = new FixedWorkplace(Guid.Empty, Guid.Empty);
        var clock = new FakeClock();
        var ensure = new EnsurePersonnelEnrichmentDefaultsUseCase(store, clock);
        var query = new OnboardingCatalogQuery(store, workplace, ensure);

        var catalog = await query.ExecuteAsync(CancellationToken.None);

        Assert.False(catalog.IsSuccess);
        Assert.Equal("workplace-not-configured", catalog.Error!.Code);
    }
}
