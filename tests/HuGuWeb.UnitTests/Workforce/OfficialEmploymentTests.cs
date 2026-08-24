using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class OfficialEmploymentTests
{
    [Fact]
    public async Task Property_CanHaveMultipleActiveSgkWorkplaceRegistrations()
    {
        var harness = new WorkforceHarness();

        var first = await harness.SgkWorkplaces.CreateAsync(
            new CreateSgkWorkplaceRegistrationCommand("111111111111111111111", "Otel"),
            CancellationToken.None);
        var second = await harness.SgkWorkplaces.CreateAsync(
            new CreateSgkWorkplaceRegistrationCommand("222222222222222222222", "Restoran"),
            CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value.Id, second.Value.Id);
        Assert.True(first.Value.IsActive);
        Assert.True(second.Value.IsActive);

        var listed = await harness.SgkWorkplaces.ListAsync(maskRegistration: false, CancellationToken.None);
        Assert.True(listed.IsSuccess);
        Assert.Equal(2, listed.Value.Count(item => item.IsActive));
    }

    [Fact]
    public async Task InactiveWorkplace_RemainsReadable_ButCannotBeNewlySelected()
    {
        var harness = new WorkforceHarness();
        var workplace = harness.SeedWorkplace();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var saved = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(
                hired.Value.EmployeeId,
                harness.OfficialWrite(workplace.Id, "01", "05510", "00", "5120.10")),
            CancellationToken.None);
        Assert.True(saved.IsSuccess);

        var deactivated = await harness.SgkWorkplaces.UpdateAsync(
            new UpdateSgkWorkplaceRegistrationCommand(workplace.Id, null, null, false, false, false),
            CancellationToken.None);
        Assert.True(deactivated.IsSuccess);
        Assert.False(deactivated.Value.IsActive);

        var card = await harness.HrCard.ExecuteAsync(hired.Value.EmployeeId, true, CancellationToken.None);
        Assert.True(card.IsSuccess);
        Assert.Equal(workplace.Id, card.Value.OfficialProfile?.SgkWorkplaceRegistrationId);

        var keep = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(
                hired.Value.EmployeeId,
                harness.OfficialWrite(workplace.Id, "01")),
            CancellationToken.None);
        Assert.True(keep.IsSuccess);

        var other = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(other.IsSuccess);
        var rejected = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(other.Value.EmployeeId, harness.OfficialWrite(workplace.Id)),
            CancellationToken.None);
        Assert.False(rejected.IsSuccess);
        Assert.Equal("sgk-workplace-inactive", rejected.Error!.Code);
    }

    [Fact]
    public async Task OfficialProfile_IsOptional_AndBelongsToEmployment()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True(hired.IsSuccess);
        Assert.Empty(harness.Store.OfficialEmploymentProfiles);

        var empty = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(hired.Value.EmployeeId, OfficialEmploymentWriteModel.Empty),
            CancellationToken.None);
        Assert.True(empty.IsSuccess);
        Assert.Single(harness.Store.OfficialEmploymentProfiles);
        Assert.Equal(hired.Value.EmploymentId, harness.Store.OfficialEmploymentProfiles[0].EmploymentId);
        Assert.True(harness.Store.OfficialEmploymentProfiles[0].IsEmpty);
    }

    [Fact]
    public async Task ValidWorkplaceForSameProperty_IsAccepted()
    {
        var harness = new WorkforceHarness();
        var workplace = harness.SeedWorkplace();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(officialProfile: harness.OfficialWrite(workplace.Id, "01", "05510", "00", "5120.10")),
            CancellationToken.None);

        Assert.True(hired.IsSuccess);
        var profile = harness.Store.OfficialEmploymentProfiles.Single();
        Assert.Equal(hired.Value.EmploymentId, profile.EmploymentId);
        Assert.Equal(workplace.Id, profile.SgkWorkplaceRegistrationId);
        Assert.Equal("01", profile.DocumentTypeCode);
        Assert.Equal("05510", profile.ApplicableLawCode);
        Assert.Equal("00", profile.InsuranceBranchCode);
        Assert.Equal("5120.10", profile.OccupationCode);
    }

    [Fact]
    public async Task WorkplaceFromDifferentProperty_IsRejected()
    {
        var harness = new WorkforceHarness();
        var other = harness.SeedWorkplace(harness.OtherPropertyId, "999999999999999999999", "Other");
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var result = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(hired.Value.EmployeeId, harness.OfficialWrite(other.Id)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("sgk-workplace-not-for-property", result.Error!.Code);
    }

    [Fact]
    public async Task MissingWorkplace_IsRejected()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        var result = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(
                hired.Value!.EmployeeId,
                harness.OfficialWrite(Guid.CreateVersion7())),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("sgk-workplace-not-found", result.Error!.Code);
    }

    [Fact]
    public async Task LookupMembership_IsValidated()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);

        var document = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(hired.Value!.EmployeeId, harness.OfficialWrite(documentType: "99")),
            CancellationToken.None);
        Assert.Equal("invalid-document-type-code", document.Error!.Code);

        var law = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(hired.Value.EmployeeId, harness.OfficialWrite(law: "99999")),
            CancellationToken.None);
        Assert.Equal("invalid-applicable-law-code", law.Error!.Code);

        var insurance = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(hired.Value.EmployeeId, harness.OfficialWrite(insurance: "99")),
            CancellationToken.None);
        Assert.Equal("invalid-insurance-branch-code", insurance.Error!.Code);

        var occupation = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(hired.Value.EmployeeId, harness.OfficialWrite(occupation: "9999.99")),
            CancellationToken.None);
        Assert.Equal("invalid-occupation-code", occupation.Error!.Code);

        var format = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(hired.Value.EmployeeId, harness.OfficialWrite(occupation: "5120")),
            CancellationToken.None);
        Assert.Equal("invalid-occupation-code", format.Error!.Code);
    }

    [Fact]
    public async Task InactiveLookup_CannotBeNewlySelected_ButExistingReferenceRemains()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True((await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(hired.Value!.EmployeeId, harness.OfficialWrite(documentType: "01")),
            CancellationToken.None)).IsSuccess);

        harness.Store.SgkDocumentTypes.Single(item => item.Code == "01").Deactivate();

        var keep = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(hired.Value.EmployeeId, harness.OfficialWrite(documentType: "01")),
            CancellationToken.None);
        Assert.True(keep.IsSuccess);

        var other = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        var rejected = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(other.Value!.EmployeeId, harness.OfficialWrite(documentType: "01")),
            CancellationToken.None);
        Assert.Equal("invalid-document-type-code", rejected.Error!.Code);
    }

    [Fact]
    public void Position_DoesNotOwnOccupationCode()
    {
        Assert.Null(typeof(Position).GetProperty("OccupationCode"));
        Assert.Null(typeof(Position).GetProperty("OccupationCodeId"));
        Assert.Null(typeof(Position).GetProperty("SgkOccupationCode"));
        Assert.Null(typeof(Employee).GetProperty("DocumentTypeCode"));
        Assert.Null(typeof(Employee).GetProperty("OccupationCode"));
        Assert.NotNull(typeof(OfficialEmploymentProfile).GetProperty("DutyCode"));
        Assert.Null(typeof(Position).GetProperty("DutyCode"));
    }

    [Fact]
    public async Task Rehire_GetsSeparateOfficialProfile()
    {
        var harness = new WorkforceHarness();
        var workplace = harness.SeedWorkplace();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True((await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(
                hired.Value!.EmployeeId,
                harness.OfficialWrite(workplace.Id, "01")),
            CancellationToken.None)).IsSuccess);

        var firstEmploymentId = hired.Value.EmploymentId;
        Assert.True((await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(hired.Value.EmployeeId, harness.Clock.Today),
            CancellationToken.None)).IsSuccess);

        var rehire = Employment.Open(
            Guid.CreateVersion7(),
            hired.Value.EmployeeId,
            harness.Clock.Today.AddDays(1),
            harness.Clock.Today.AddDays(1));
        var assignment = Assignment.StartPrimary(
            Guid.CreateVersion7(),
            rehire.Id,
            harness.DepartmentId,
            harness.PositionId,
            rehire.StartDate);
        harness.Store.AddEmployment(rehire);
        harness.Store.AddAssignment(assignment);

        var second = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(
                hired.Value.EmployeeId,
                harness.OfficialWrite(workplace.Id, "02", "00000", "08", "1411.08")),
            CancellationToken.None);
        Assert.True(second.IsSuccess);
        Assert.Equal(2, harness.Store.OfficialEmploymentProfiles.Count);
        Assert.Equal("01", harness.Store.OfficialEmploymentProfiles.Single(item => item.EmploymentId == firstEmploymentId).DocumentTypeCode);
        Assert.Equal("02", harness.Store.OfficialEmploymentProfiles.Single(item => item.EmploymentId == rehire.Id).DocumentTypeCode);
        Assert.Equal(rehire.Id, second.Value.EmploymentId);
    }

    [Fact]
    public async Task FormerEmployeeOfficialProfile_RemainsReadable()
    {
        var harness = new WorkforceHarness();
        var workplace = harness.SeedWorkplace();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True((await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(
                hired.Value!.EmployeeId,
                harness.OfficialWrite(workplace.Id, "01", occupation: "5120.10")),
            CancellationToken.None)).IsSuccess);

        Assert.True((await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(hired.Value.EmployeeId, harness.Clock.Today),
            CancellationToken.None)).IsSuccess);

        var card = await harness.HrCard.ExecuteAsync(hired.Value.EmployeeId, true, CancellationToken.None);
        Assert.True(card.IsSuccess);
        Assert.Equal(hired.Value.EmploymentId, card.Value.OfficialProfile?.EmploymentId);
        Assert.Equal("01", card.Value.OfficialProfile?.DocumentTypeCode);
        Assert.Equal("5120.10", card.Value.OfficialProfile?.OccupationCode);
    }

    [Fact]
    public async Task EmptyOfficialSave_DoesNotSubmitOrChangeEmployment()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        var employment = harness.Store.Employments.Single();
        var assignment = harness.Store.Assignments.Single();

        var result = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(hired.Value!.EmployeeId, OfficialEmploymentWriteModel.Empty),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(employment.Id, harness.Store.Employments.Single().Id);
        Assert.Equal(employment.Status, harness.Store.Employments.Single().Status);
        Assert.Equal(assignment.Id, harness.Store.Assignments.Single().Id);
        Assert.Equal(assignment.DepartmentId, harness.Store.Assignments.Single().DepartmentId);
        Assert.Equal(assignment.PositionId, harness.Store.Assignments.Single().PositionId);
        Assert.DoesNotContain(
            typeof(OfficialEmploymentProfile).GetProperties().Select(property => property.Name),
            name => name.Contains("Submit", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Notification", StringComparison.OrdinalIgnoreCase)
                || name.Contains("SgkStatus", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            harness.Store.GetType().GetProperties(),
            property =>
                property.Name.Contains("Notification", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Outbox", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task OrdinaryCardSave_DoesNotCreateOfficialProfileWhenBlank()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var updated = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value.EmployeeId,
                "Ayşe",
                "Yılmaz",
                WorkforceHarness.EmptyProfile(),
                true,
                OfficialEmploymentWriteModel.Empty),
            CancellationToken.None);

        Assert.True(updated.IsSuccess);
        Assert.Empty(harness.Store.OfficialEmploymentProfiles);
    }

    [Fact]
    public async Task OccupationSearch_MatchesCodeOrDescription()
    {
        var harness = new WorkforceHarness();
        var byCode = await harness.OfficialLookups.SearchOccupationsAsync("5120", CancellationToken.None);
        Assert.Contains(byCode.Value!, item => item.Code == "5120.10");

        var byName = await harness.OfficialLookups.SearchOccupationsAsync("Aşçı", CancellationToken.None);
        Assert.Contains(byName.Value!, item => item.Code == "5120.10");
        Assert.True(byName.Value!.Count <= OfficialLookupsQuery.OccupationSearchLimit);
    }

    [Fact]
    public async Task EmployeeNotFound_ReturnsEmploymentOrEmployeeError()
    {
        var harness = new WorkforceHarness();
        var result = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(Guid.CreateVersion7(), OfficialEmploymentWriteModel.Empty),
            CancellationToken.None);
        Assert.Equal("employee-not-found", result.Error!.Code);
    }
}
