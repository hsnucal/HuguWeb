using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class PersonnelEnrichmentRecruitmentSourceTests
{
    [Fact]
    public async Task EnsureDefaults_SeedsFiveSourcesIdempotentlyIncludingReferral()
    {
        var harness = new WorkforceHarness();
        var first = await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var second = await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);

        Assert.True(first >= RecruitmentSourceDefaults.All.Count);
        Assert.Equal(0, second);
        Assert.Equal(5, harness.Store.RecruitmentSources.Count);
        Assert.Contains(harness.Store.RecruitmentSources, item => item.Code == "LINKEDIN");
        Assert.Contains(harness.Store.RecruitmentSources, item => item.Code == "DIRECT_APPLICATION");
        Assert.Contains(harness.Store.RecruitmentSources, item => item.Code == "REFERRAL");
    }

    [Fact]
    public async Task Hire_RejectsUnknownRecruitmentSource()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(
                workforceTerms: new EmploymentWorkforceWriteModel(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    WorkType.FullTime,
                    RecruitmentSourceId: Guid.CreateVersion7())),
            CancellationToken.None);

        Assert.False(hired.IsSuccess);
        Assert.Equal(HrValidation.Codes.RecruitmentSourceNotFound, hired.Error!.Code);
    }

    [Fact]
    public async Task Hire_AcceptsActiveRecruitmentSource()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var source = harness.Store.RecruitmentSources.Single(item => item.Code == "LINKEDIN");

        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(
                workforceTerms: new EmploymentWorkforceWriteModel(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    WorkType.FullTime,
                    RecruitmentSourceId: source.Id)),
            CancellationToken.None);

        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        Assert.Equal(source.Id, harness.Store.Employments[0].RecruitmentSourceId);
        var listed = await harness.ListRecruitmentSources.ExecuteAsync(true, CancellationToken.None);
        Assert.True(listed.IsSuccess);
        Assert.Contains(listed.Value!, item => item.Id == source.Id);
    }

    [Fact]
    public async Task Update_AllowsKeepingCurrentInactiveRecruitmentSource()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var source = harness.Store.RecruitmentSources.Single(item => item.Code == "LINKEDIN");
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(
                workforceTerms: new EmploymentWorkforceWriteModel(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    WorkType.FullTime,
                    RecruitmentSourceId: source.Id)),
            CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);

        source.Deactivate(harness.Clock.UtcNow);
        var updated = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId,
                hired.Value.GivenName,
                hired.Value.FamilyName,
                EmptyProfile(),
                CanWriteSensitive: true,
                WorkforceTerms: new EmploymentWorkforceWriteModel(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    WorkType.FullTime,
                    RecruitmentSourceId: source.Id)),
            CancellationToken.None);

        Assert.True(updated.IsSuccess, updated.Error?.Detail);
        Assert.Equal(source.Id, harness.Store.Employments[0].RecruitmentSourceId);
    }

    [Fact]
    public async Task Update_RejectsNewlyChosenInactiveRecruitmentSource()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var linkedIn = harness.Store.RecruitmentSources.Single(item => item.Code == "LINKEDIN");
        var kariyer = harness.Store.RecruitmentSources.Single(item => item.Code == "KARIYER_NET");
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(
                workforceTerms: new EmploymentWorkforceWriteModel(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    WorkType.FullTime,
                    RecruitmentSourceId: linkedIn.Id)),
            CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);

        kariyer.Deactivate(harness.Clock.UtcNow);
        var updated = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId,
                hired.Value.GivenName,
                hired.Value.FamilyName,
                EmptyProfile(),
                CanWriteSensitive: true,
                WorkforceTerms: new EmploymentWorkforceWriteModel(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    WorkType.FullTime,
                    RecruitmentSourceId: kariyer.Id)),
            CancellationToken.None);

        Assert.False(updated.IsSuccess);
        Assert.Equal(HrValidation.Codes.RecruitmentSourceInactive, updated.Error!.Code);
    }

    private static HrProfileWriteModel EmptyProfile() =>
        new(
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, []);
}
