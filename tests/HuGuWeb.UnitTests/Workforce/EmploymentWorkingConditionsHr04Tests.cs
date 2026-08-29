using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class EmploymentWorkingConditionsHr04Tests
{
    [Fact]
    public void SeniorityStartDate_Null_IsAllowed()
    {
        var employment = Open();

        Assert.True(employment.TryApplySeniorityStartDate(null, out _, out _));
        Assert.Null(employment.SeniorityStartDate);
        Assert.Equal(employment.StartDate, employment.EffectiveSeniorityDate);
    }

    [Fact]
    public void SeniorityStartDate_OnOrBeforeStartDate_IsAccepted()
    {
        var employment = Open(start: new DateOnly(2026, 8, 21));

        Assert.True(employment.TryApplySeniorityStartDate(new DateOnly(2026, 8, 1), out _, out _));
        Assert.Equal(new DateOnly(2026, 8, 1), employment.SeniorityStartDate);
        Assert.True(employment.TryApplySeniorityStartDate(new DateOnly(2026, 8, 21), out _, out _));
        Assert.Equal(employment.StartDate, employment.SeniorityStartDate);
    }

    [Fact]
    public void SeniorityStartDate_AfterStartDate_IsRejected()
    {
        var employment = Open(start: new DateOnly(2026, 8, 21));

        Assert.False(employment.TryApplySeniorityStartDate(new DateOnly(2026, 8, 22), out var field, out var code));
        Assert.Null(employment.SeniorityStartDate);
        Assert.Equal(HrValidation.Fields.SeniorityStartDate, field);
        Assert.Equal(HrValidation.Codes.SeniorityStartDateInvalid, code);
    }

    [Fact]
    public async Task Hire_DoesNotCopyStartDateIntoSeniorityStartDate()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);

        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        Assert.Null(harness.Store.Employments[0].SeniorityStartDate);
        Assert.Equal(harness.Store.Employments[0].StartDate, harness.Store.Employments[0].EffectiveSeniorityDate);
    }

    [Fact]
    public async Task EndEmployment_RequiresTerminationReason()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        var result = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(hired.Value!.EmployeeId, harness.Clock.Today, default),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(HrValidation.Codes.TerminationReasonRequired, result.Error!.Code);
        Assert.False(harness.Store.Employments[0].IsEnded);
        Assert.Null(harness.Store.Employments[0].TerminationReason);
        Assert.Null(harness.Store.Assignments[0].EndDate);
    }

    [Theory]
    [InlineData(EmploymentTerminationReason.Resignation)]
    [InlineData(EmploymentTerminationReason.EmployerTermination)]
    [InlineData(EmploymentTerminationReason.ContractEnded)]
    [InlineData(EmploymentTerminationReason.Retirement)]
    [InlineData(EmploymentTerminationReason.Other)]
    public async Task EndEmployment_AcceptsEachValidTerminationReason(EmploymentTerminationReason reason)
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(
            harness.HireCommand(startDate: harness.Clock.Today.AddDays(-2)),
            CancellationToken.None);

        var result = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(hired.Value!.EmployeeId, harness.Clock.Today, reason),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(reason, result.Value!.TerminationReason);
        Assert.Equal(reason, harness.Store.Employments[0].TerminationReason);
        Assert.Equal(EmploymentStatus.Ended, harness.Store.Employments[0].Status);
        Assert.Equal(harness.Clock.Today, harness.Store.Employments[0].EndDate);
        Assert.Equal(harness.Clock.Today, harness.Store.Assignments[0].EndDate);
    }

    [Fact]
    public async Task EndEmployment_PersistsTerminationReasonAtomically()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(
            harness.HireCommand(startDate: harness.Clock.Today.AddDays(-3)),
            CancellationToken.None);

        var result = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(
                hired.Value!.EmployeeId,
                harness.Clock.Today,
                EmploymentTerminationReason.EmployerTermination),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        var employment = harness.Store.Employments[0];
        Assert.Equal(EmploymentStatus.Ended, employment.Status);
        Assert.Equal(EmploymentTerminationReason.EmployerTermination, employment.TerminationReason);
        Assert.Equal(harness.Clock.Today, employment.EndDate);
        Assert.Equal(harness.Clock.Today, harness.Store.Assignments[0].EndDate);

        var history = await harness.History.ExecuteAsync(hired.Value.EmployeeId, CancellationToken.None);
        Assert.Equal(EmploymentTerminationReason.EmployerTermination, history.Value!.Employments[0].TerminationReason);
    }

    [Fact]
    public async Task EndEmployment_RejectsUndefinedTerminationReason()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        var result = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(
                hired.Value!.EmployeeId,
                harness.Clock.Today,
                (EmploymentTerminationReason)99),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(HrValidation.Codes.InvalidTerminationReason, result.Error!.Code);
        Assert.False(harness.Store.Employments[0].IsEnded);
        Assert.Null(harness.Store.Employments[0].TerminationReason);
    }

    [Fact]
    public async Task OrdinaryProfileUpdate_CannotChangeEndDateStatusOrTerminationReason()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        var employment = harness.Store.Employments[0];
        var originalEnd = employment.EndDate;
        var originalStatus = employment.Status;
        var originalReason = employment.TerminationReason;

        var updated = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId,
                "Ayşe",
                "Yılmaz",
                WorkforceHarness.EmptyProfile(),
                true,
                OfficialEmploymentWriteModel.Empty,
                new EmploymentWorkforceWriteModel(
                    EmploymentContractType.Indefinite,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null),
                SeniorityStartDate: hired.Value.EmploymentStartDate.AddDays(-10),
                ApplySeniorityStartDate: true),
            CancellationToken.None);

        Assert.True(updated.IsSuccess, updated.Error?.Detail);
        Assert.Equal(originalEnd, employment.EndDate);
        Assert.Equal(originalStatus, employment.Status);
        Assert.Equal(originalReason, employment.TerminationReason);
        Assert.Null(employment.TerminationReason);
        Assert.False(employment.IsEnded);
        Assert.Equal(hired.Value.EmploymentStartDate.AddDays(-10), employment.SeniorityStartDate);
        Assert.Equal(EmploymentContractType.Indefinite, employment.ContractType);
    }

    [Fact]
    public async Task EndEmployment_StillClosesOpenPrimaryAssignment()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(
            harness.HireCommand(startDate: harness.Clock.Today.AddDays(-5)),
            CancellationToken.None);
        var assignmentId = hired.Value!.AssignmentId;

        var result = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(
                hired.Value.EmployeeId,
                harness.Clock.Today,
                EmploymentTerminationReason.ContractEnded),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        var assignment = harness.Store.Assignments.Single(item => item.Id == assignmentId);
        Assert.Equal(harness.Clock.Today, assignment.EndDate);
        Assert.True(harness.Store.Employments[0].IsEnded);
    }

    [Fact]
    public async Task AlreadyEndedEmployment_CannotBeEndedAgain()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        Assert.True((await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(
                hired.Value!.EmployeeId,
                harness.Clock.Today,
                EmploymentTerminationReason.Resignation),
            CancellationToken.None)).IsSuccess);

        var again = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(
                hired.Value.EmployeeId,
                harness.Clock.Today,
                EmploymentTerminationReason.Other),
            CancellationToken.None);

        Assert.False(again.IsSuccess);
        Assert.Equal("no-current-employment", again.Error!.Code);
        Assert.Equal(EmploymentTerminationReason.Resignation, harness.Store.Employments[0].TerminationReason);
        Assert.Single(harness.Store.Employments);
    }

    [Fact]
    public async Task TransferHistory_IsUnchangedByWorkingConditions()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(
            harness.HireCommand(startDate: harness.Clock.Today.AddDays(-10)),
            CancellationToken.None);
        var originalAssignmentId = hired.Value!.AssignmentId;
        var effectiveDate = harness.Clock.Today;

        var transferred = await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                hired.Value.EmployeeId,
                harness.OtherDepartmentId,
                harness.OtherPositionId,
                effectiveDate),
            CancellationToken.None);

        Assert.True(transferred.IsSuccess, transferred.Error?.Detail);
        Assert.Equal(2, harness.Store.Assignments.Count);
        var previous = harness.Store.Assignments.Single(item => item.Id == originalAssignmentId);
        var next = harness.Store.Assignments.Single(item => item.Id != originalAssignmentId);
        Assert.Equal(effectiveDate.AddDays(-1), previous.EndDate);
        Assert.Equal(effectiveDate, next.StartDate);
        Assert.Null(next.EndDate);
        Assert.False(PrimaryAssignments.HasOverlap(harness.Store.Assignments));

        var history = await harness.History.ExecuteAsync(hired.Value.EmployeeId, CancellationToken.None);
        Assert.Equal(2, history.Value!.Employments[0].PrimaryAssignments.Count);
        Assert.Contains(history.Value.Employments[0].PrimaryAssignments, item => item.Id == originalAssignmentId);
    }

    [Fact]
    public void EmployeeAndEmployment_DoNotHavePropertyId()
    {
        Assert.Null(typeof(Employee).GetProperty("PropertyId"));
        Assert.Null(typeof(Employment).GetProperty("PropertyId"));
        Assert.Null(typeof(Employee).GetProperty("ManagerId"));
        Assert.DoesNotContain(
            typeof(Employee).Assembly.GetTypes().Select(type => type.Name),
            name => name is "EmploymentContract" or "EmployeeReportingLine");
    }

    [Fact]
    public async Task ContractFields_RemainOwnedByEmployment()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);

        var updated = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId,
                "Ayşe",
                "Yılmaz",
                WorkforceHarness.EmptyProfile(),
                true,
                OfficialEmploymentWriteModel.Empty,
                new EmploymentWorkforceWriteModel(
                    EmploymentContractType.PartTime,
                    null,
                    80m,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null)),
            CancellationToken.None);

        Assert.True(updated.IsSuccess, updated.Error?.Detail);
        var employment = harness.Store.Employments.Single();
        Assert.Equal(EmploymentContractType.PartTime, employment.ContractType);
        Assert.Equal(80m, employment.PartTimeMonthlyHours);
        Assert.Null(employment.ContractEndDate);
        Assert.Null(typeof(Employee).GetProperty("ContractType"));
        Assert.Null(typeof(Employee).GetProperty("ContractEndDate"));
        Assert.Null(typeof(Employee).GetProperty("PartTimeMonthlyHours"));
        Assert.Null(typeof(Employee).GetProperty("SeniorityStartDate"));
        Assert.Null(typeof(Employee).GetProperty("TerminationReason"));
    }

    [Fact]
    public async Task ContractEndDate_BeforeEmploymentStart_IsRejected()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(startDate: new DateOnly(2026, 8, 21)),
            CancellationToken.None);

        var result = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId,
                "Ayşe",
                "Yılmaz",
                WorkforceHarness.EmptyProfile(),
                true,
                OfficialEmploymentWriteModel.Empty,
                new EmploymentWorkforceWriteModel(
                    EmploymentContractType.FixedTerm,
                    new DateOnly(2026, 8, 1),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(HrValidation.Codes.ContractEndDateBeforeStart, result.Error!.Code);
        Assert.Null(harness.Store.Employments[0].ContractType);
    }

    [Fact]
    public async Task ProfileHistory_RecordsSeniorityAndContractChanges()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        var actor = new PersonnelChangeContext(
            "user-1",
            null,
            harness.OrganizationId,
            harness.PropertyId,
            harness.Clock.UtcNow);

        var updated = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId,
                "Ayşe",
                "Yılmaz",
                WorkforceHarness.EmptyProfile(),
                true,
                OfficialEmploymentWriteModel.Empty,
                new EmploymentWorkforceWriteModel(
                    EmploymentContractType.Indefinite,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null),
                ChangeContext: actor,
                SeniorityStartDate: hired.Value.EmploymentStartDate.AddDays(-30),
                ApplySeniorityStartDate: true),
            CancellationToken.None);

        Assert.True(updated.IsSuccess, updated.Error?.Detail);
        var codes = harness.Store.ProfileChanges.Select(item => item.FieldCode).ToArray();
        Assert.Contains(PersonnelProfileFieldCodes.SeniorityStartDate, codes);
        Assert.Contains(PersonnelProfileFieldCodes.ContractType, codes);
    }

    [Fact]
    public void TerminationReason_IsClosedHuGuCodes_NotSgkEk2()
    {
        var names = Enum.GetNames<EmploymentTerminationReason>();
        Assert.Equal(
            ["Resignation", "EmployerTermination", "ContractEnded", "Retirement", "Other"],
            names);
        Assert.DoesNotContain(names, name => name.Contains("Ek2", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("Sgk", StringComparison.OrdinalIgnoreCase));
    }

    private static Employment Open(DateOnly? start = null) =>
        Employment.Open(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            start ?? new DateOnly(2026, 8, 21),
            new DateOnly(2026, 8, 21));
}
