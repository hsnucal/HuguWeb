using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public sealed class PersonnelFinding07OnboardingDraftTests
{
    [Fact]
    public async Task PreviewDraft_RendersPersonnelValues_WithoutEmployeeOrEmployment()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);

        var employeeCountBefore = harness.Store.Employees.Count;
        var employmentCountBefore = harness.Store.Employments.Count;
        var statusCountBefore = harness.Store.EmploymentOnboardingDocumentStatuses.Count;

        var template = harness.Store.HrDocumentTemplates.Single(item => item.Code == "OVERTIME-CONSENT");
        var preview = await harness.PreviewHrDocumentTemplateDraft.ExecuteAsync(
            template.Id,
            new HrOnboardingDocumentDraftRequest(
                "Hasan",
                "Uçal",
                "2026-09-02",
                null,
                null),
            CancellationToken.None);

        Assert.True(preview.IsSuccess, preview.Error?.Detail);
        Assert.Contains("Hasan Uçal", preview.Value!.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("02.09.2026", preview.Value.RenderedContent, StringComparison.Ordinal);
        Assert.Equal(employeeCountBefore, harness.Store.Employees.Count);
        Assert.Equal(employmentCountBefore, harness.Store.Employments.Count);
        Assert.Equal(statusCountBefore, harness.Store.EmploymentOnboardingDocumentStatuses.Count);
    }

    [Fact]
    public async Task PreviewDraft_RejectsMissingRequiredFields()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var template = harness.Store.HrDocumentTemplates.Single(item => item.Code == "OVERTIME-CONSENT");

        var preview = await harness.PreviewHrDocumentTemplateDraft.ExecuteAsync(
            template.Id,
            new HrOnboardingDocumentDraftRequest("Hasan", "", "2026-09-02", null, null),
            CancellationToken.None);

        Assert.False(preview.IsSuccess);
        Assert.Equal(HrValidation.Codes.FamilyNameRequired, preview.Error!.Errors![HrValidation.Fields.FamilyName][0]);
    }

    [Fact]
    public async Task PreviewDraft_RejectsOtherOrganizationTemplate()
    {
        var harness = new WorkforceHarness();
        var otherOrgId = Guid.CreateVersion7();
        harness.Store.Organizations.Add(new Organization(otherOrgId, "Other Organization"));
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(otherOrgId, CancellationToken.None);
        var otherTemplate = harness.Store.HrDocumentTemplates.Single(item => item.OrganizationId == otherOrgId);

        var preview = await harness.PreviewHrDocumentTemplateDraft.ExecuteAsync(
            otherTemplate.Id,
            new HrOnboardingDocumentDraftRequest("Hasan", "Uçal", "2026-09-02", null, null),
            CancellationToken.None);

        Assert.False(preview.IsSuccess);
        Assert.Equal(HrValidation.Codes.DocumentTemplateNotFound, preview.Error!.Code);
    }

    [Fact]
    public async Task GenerateDraftDocx_ReturnsDocx_WithoutPersistingEmployee()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var template = harness.Store.HrDocumentTemplates.Single(item => item.Code == "OVERTIME-CONSENT");
        var employeeCountBefore = harness.Store.Employees.Count;

        var docx = await harness.RenderHrDocumentDraftDocx.ExecuteAsync(
            template.Id,
            new HrOnboardingDocumentDraftRequest("Hasan", "Uçal", "2026-09-02", null, null),
            CancellationToken.None);

        Assert.True(docx.IsSuccess, docx.Error?.Detail);
        Assert.True(docx.Value!.Content.Length > 0);
        Assert.Equal(employeeCountBefore, harness.Store.Employees.Count);
    }

    [Fact]
    public async Task CompletedEmployment_StillRejectsPersistedGeneration()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);
        Assert.True((await harness.CompleteEmploymentOnboarding.ExecuteAsync(
            hired.Value!.EmployeeId,
            CancellationToken.None)).IsSuccess);

        var template = harness.Store.HrDocumentTemplates.Single(item => item.Code == "OVERTIME-CONSENT");
        var docx = await harness.RenderHrDocumentDocx.ExecuteAsync(
            hired.Value.EmployeeId,
            template.Id,
            CancellationToken.None);
        Assert.False(docx.IsSuccess);
        Assert.Equal(HrValidation.Codes.OnboardingDocumentGenerationClosed, docx.Error!.Code);
    }
}
