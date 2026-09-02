using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class PersonnelEnrichmentOnboardingChecklistTests
{
    [Fact]
    public async Task EnsureDefaults_SeedsSevenRequirements()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);

        Assert.Equal(7, harness.Store.OnboardingDocumentRequirements.Count);
        Assert.Contains(harness.Store.OnboardingDocumentRequirements, item => item.Code == "ID_COPY");
        Assert.Contains(harness.Store.OnboardingDocumentRequirements, item => item.Code == "BANK_IBAN");
    }

    [Fact]
    public async Task Checklist_StartsIncomplete_AndCanToggle()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var checklist = await harness.OnboardingChecklist.ExecuteAsync(
            hired.Value!.EmployeeId,
            CancellationToken.None);
        Assert.True(checklist.IsSuccess, checklist.Error?.Detail);
        Assert.Equal(7, checklist.Value!.TotalCount);
        Assert.Equal(0, checklist.Value.CompletedCount);

        var requirement = harness.Store.OnboardingDocumentRequirements.Single(item => item.Code == "PHOTO");
        var completed = await harness.SetOnboardingChecklistItem.ExecuteAsync(
            new SetOnboardingChecklistItemCommand(
                hired.Value.EmployeeId,
                requirement.Id,
                IsCompleted: true,
                "actor-1"),
            CancellationToken.None);
        Assert.True(completed.IsSuccess, completed.Error?.Detail);
        Assert.True(completed.Value!.IsCompleted);
        Assert.Equal("actor-1", completed.Value.CompletedByUserId);

        var after = await harness.OnboardingChecklist.ExecuteAsync(
            hired.Value.EmployeeId,
            CancellationToken.None);
        Assert.Equal(1, after.Value!.CompletedCount);

        var cleared = await harness.SetOnboardingChecklistItem.ExecuteAsync(
            new SetOnboardingChecklistItemCommand(
                hired.Value.EmployeeId,
                requirement.Id,
                IsCompleted: false,
                "actor-1"),
            CancellationToken.None);
        Assert.True(cleared.IsSuccess);
        Assert.False(cleared.Value!.IsCompleted);
        Assert.Null(cleared.Value.CompletedByUserId);
    }

    [Fact]
    public async Task NewEmployment_StartsOnboardingInProgress_AndFinalizeLocksChecklist()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var open = await harness.OnboardingChecklist.ExecuteAsync(
            hired.Value!.EmployeeId,
            CancellationToken.None);
        Assert.True(open.IsSuccess);
        Assert.Equal(nameof(EmploymentOnboardingStatus.InProgress), open.Value!.OnboardingStatus);
        Assert.True(open.Value.CanEditChecklist);
        Assert.True(open.Value.CanGenerateDocuments);

        var requirement = harness.Store.OnboardingDocumentRequirements.Single(item => item.Code == "ID_COPY");
        var toggled = await harness.SetOnboardingChecklistItem.ExecuteAsync(
            new SetOnboardingChecklistItemCommand(
                hired.Value.EmployeeId,
                requirement.Id,
                IsCompleted: true,
                "actor-1"),
            CancellationToken.None);
        Assert.True(toggled.IsSuccess);

        var completed = await harness.CompleteEmploymentOnboarding.ExecuteAsync(
            hired.Value.EmployeeId,
            CancellationToken.None);
        Assert.True(completed.IsSuccess, completed.Error?.Detail);
        Assert.Equal(nameof(EmploymentOnboardingStatus.Completed), completed.Value!.OnboardingStatus);
        Assert.False(completed.Value.CanEditChecklist);
        Assert.False(completed.Value.CanGenerateDocuments);

        var blocked = await harness.SetOnboardingChecklistItem.ExecuteAsync(
            new SetOnboardingChecklistItemCommand(
                hired.Value.EmployeeId,
                requirement.Id,
                IsCompleted: false,
                "actor-1"),
            CancellationToken.None);
        Assert.False(blocked.IsSuccess);
        Assert.Equal(HrValidation.Codes.OnboardingDocumentsReadOnly, blocked.Error!.Code);

        var alreadyDone = await harness.CompleteEmploymentOnboarding.ExecuteAsync(
            hired.Value.EmployeeId,
            CancellationToken.None);
        Assert.False(alreadyDone.IsSuccess);
        Assert.Equal(HrValidation.Codes.OnboardingAlreadyCompleted, alreadyDone.Error!.Code);

        var readOnlyView = await harness.OnboardingChecklist.ExecuteAsync(
            hired.Value.EmployeeId,
            CancellationToken.None);
        Assert.True(readOnlyView.IsSuccess);
        Assert.False(readOnlyView.Value!.CanEditChecklist);
        Assert.False(readOnlyView.Value.CanGenerateDocuments);
        Assert.Equal(1, readOnlyView.Value.CompletedCount);
    }

    [Fact]
    public async Task SyncOnboardingChecklist_PersistsDraftState_OnHire()
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
        var residence = harness.Store.OnboardingDocumentRequirements.Single(item => item.Code == "RESIDENCE");

        var synced = await harness.SyncOnboardingChecklist.ExecuteAsync(
            new SyncOnboardingChecklistCommand(
                hired.Value!.EmployeeId,
                [idCopy.Id, residence.Id],
                "actor"),
            CancellationToken.None);
        Assert.True(synced.IsSuccess, synced.Error?.Detail);
        Assert.Equal(2, synced.Value!.CompletedCount);
        Assert.True(synced.Value.CanEditChecklist);
        Assert.True(synced.Value.CanGenerateDocuments);
    }

    [Fact]
    public async Task Docx_Render_BlockedAfterOnboardingCompleted()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var completed = await harness.CompleteEmploymentOnboarding.ExecuteAsync(
            hired.Value!.EmployeeId,
            CancellationToken.None);
        Assert.True(completed.IsSuccess);

        var template = harness.Store.HrDocumentTemplates.Single(item => item.Code == "OVERTIME-CONSENT");
        var docx = await harness.RenderHrDocumentDocx.ExecuteAsync(
            hired.Value.EmployeeId,
            template.Id,
            CancellationToken.None);
        Assert.False(docx.IsSuccess);
        Assert.Equal(HrValidation.Codes.OnboardingDocumentGenerationClosed, docx.Error!.Code);

        var preview = await harness.PreviewHrDocumentTemplate.ExecuteAsync(
            hired.Value.EmployeeId,
            template.Id,
            CancellationToken.None);
        Assert.False(preview.IsSuccess);
        Assert.Equal(HrValidation.Codes.OnboardingDocumentGenerationClosed, preview.Error!.Code);
    }

    [Fact]
    public async Task Docx_Render_UsesPersonnelData_DuringInProgressOnboarding()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var hired = await harness.HireWithProfile.ExecuteAsync(
            new HireEmployeeWithProfileCommand(
                "Ali",
                "Tekin",
                harness.Clock.Today,
                harness.DepartmentId,
                harness.PositionId,
                WorkforceHarness.EmptyProfile(),
                CanWriteSensitive: true),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var template = harness.Store.HrDocumentTemplates.Single(item => item.Code == "OVERTIME-CONSENT");
        var docx = await harness.RenderHrDocumentDocx.ExecuteAsync(
            hired.Value!.EmployeeId,
            template.Id,
            CancellationToken.None);
        Assert.True(docx.IsSuccess, docx.Error?.Detail);
        Assert.EndsWith(".docx", docx.Value!.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Ali", docx.Value.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.True(docx.Value.Content.Length > 0);
    }

    [Fact]
    public async Task HistoricalChecklist_PreservesExactOnboardingState_AfterFinalize()
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
        var diploma = harness.Store.OnboardingDocumentRequirements.Single(item => item.Code == "DIPLOMA");

        Assert.True((await harness.SetOnboardingChecklistItem.ExecuteAsync(
            new SetOnboardingChecklistItemCommand(hired.Value!.EmployeeId, idCopy.Id, true, "actor"),
            CancellationToken.None)).IsSuccess);
        Assert.True((await harness.SetOnboardingChecklistItem.ExecuteAsync(
            new SetOnboardingChecklistItemCommand(hired.Value.EmployeeId, criminal.Id, true, "actor"),
            CancellationToken.None)).IsSuccess);

        var completed = await harness.CompleteEmploymentOnboarding.ExecuteAsync(
            hired.Value.EmployeeId,
            CancellationToken.None);
        Assert.True(completed.IsSuccess);

        var historical = await harness.OnboardingChecklist.ExecuteAsync(
            hired.Value.EmployeeId,
            CancellationToken.None);
        Assert.True(historical.IsSuccess);
        Assert.Equal(2, historical.Value!.CompletedCount);
        Assert.False(historical.Value.CanEditChecklist);

        var items = historical.Value.Items.ToDictionary(item => item.Code);
        Assert.True(items["ID_COPY"].IsCompleted);
        Assert.True(items["CRIMINAL_RECORD"].IsCompleted);
        Assert.False(items["DIPLOMA"].IsCompleted);
        Assert.False(items["RESIDENCE"].IsCompleted);
    }

    [Fact]
    public async Task CreateFinalize_SyncThenComplete_PreservesExactDraft_AndReopenIsReadOnly()
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
        var diploma = harness.Store.OnboardingDocumentRequirements.Single(item => item.Code == "DIPLOMA");

        var synced = await harness.SyncOnboardingChecklist.ExecuteAsync(
            new SyncOnboardingChecklistCommand(
                hired.Value!.EmployeeId,
                [idCopy.Id, criminal.Id],
                "actor"),
            CancellationToken.None);
        Assert.True(synced.IsSuccess, synced.Error?.Detail);

        var completed = await harness.CompleteEmploymentOnboarding.ExecuteAsync(
            hired.Value.EmployeeId,
            CancellationToken.None);
        Assert.True(completed.IsSuccess, completed.Error?.Detail);
        Assert.Equal(nameof(EmploymentOnboardingStatus.Completed), completed.Value!.OnboardingStatus);
        Assert.False(completed.Value.CanEditChecklist);
        Assert.False(completed.Value.CanGenerateDocuments);

        var employment = harness.Store.Employments.Single(item => item.Id == hired.Value.EmploymentId);
        Assert.Equal(EmploymentOnboardingStatus.Completed, employment.OnboardingStatus);

        var reopened = await harness.OnboardingChecklist.ExecuteAsync(
            hired.Value.EmployeeId,
            CancellationToken.None);
        Assert.True(reopened.IsSuccess);
        Assert.Equal(nameof(EmploymentOnboardingStatus.Completed), reopened.Value!.OnboardingStatus);
        Assert.False(reopened.Value.CanEditChecklist);
        Assert.Equal(2, reopened.Value.CompletedCount);

        var items = reopened.Value.Items.ToDictionary(item => item.Code);
        Assert.True(items["ID_COPY"].IsCompleted);
        Assert.True(items["CRIMINAL_RECORD"].IsCompleted);
        Assert.False(items["DIPLOMA"].IsCompleted);

        var blocked = await harness.SetOnboardingChecklistItem.ExecuteAsync(
            new SetOnboardingChecklistItemCommand(
                hired.Value.EmployeeId,
                diploma.Id,
                IsCompleted: true,
                "actor"),
            CancellationToken.None);
        Assert.False(blocked.IsSuccess);
        Assert.Equal(HrValidation.Codes.OnboardingDocumentsReadOnly, blocked.Error!.Code);
    }
}
