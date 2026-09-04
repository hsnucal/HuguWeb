using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class WorkforceMovementHr08ATests
{
    [Fact]
    public async Task Transfer_DepartmentOnly_RecordsDepartmentChangeMovement()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var result = await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                hired.EmployeeId,
                harness.OtherDepartmentId,
                harness.PositionId,
                harness.Clock.Today),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        var movement = Assert.Single(harness.Store.PersonnelMovements);
        Assert.Equal(PersonnelMovementType.DepartmentChange, movement.MovementType);
        Assert.Equal(result.Value!.ClosedAssignmentId, movement.PreviousAssignmentId);
        Assert.Equal(result.Value.NewAssignmentId, movement.NewAssignmentId);
    }

    [Fact]
    public async Task Transfer_PositionOnly_RecordsPositionChangeMovement()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var result = await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                hired.EmployeeId,
                harness.DepartmentId,
                harness.OtherPositionId,
                harness.Clock.Today),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("position-not-available-for-department", result.Error!.Code);

        harness.AddApplicability(harness.DepartmentId, harness.OtherPositionId);
        result = await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                hired.EmployeeId,
                harness.DepartmentId,
                harness.OtherPositionId,
                harness.Clock.Today),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(PersonnelMovementType.PositionChange, Assert.Single(harness.Store.PersonnelMovements).MovementType);
    }

    [Fact]
    public async Task Transfer_DepartmentAndPosition_RecordsAssignmentChange_NotPromotion()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var result = await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                hired.EmployeeId,
                harness.OtherDepartmentId,
                harness.OtherPositionId,
                harness.Clock.Today),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        var movement = Assert.Single(harness.Store.PersonnelMovements);
        Assert.Equal(PersonnelMovementType.AssignmentChange, movement.MovementType);
        Assert.NotEqual(PersonnelMovementType.Promotion, movement.MovementType);
    }

    [Fact]
    public async Task DepartmentChange_KeepsApplicablePosition()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var created = await harness.CreateMovement.ExecuteAsync(
            DepartmentChange(hired.EmploymentId, harness.OtherDepartmentId, targetPositionId: null),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error?.Detail);
        Assert.Equal(PersonnelMovementType.DepartmentChange, created.Value!.Type);
        Assert.Equal(harness.PositionId, created.Value.NewAssignment!.PositionId);
        Assert.Equal(harness.OtherDepartmentId, created.Value.NewAssignment.DepartmentId);
    }

    [Fact]
    public async Task DepartmentChange_RequiresTargetPositionWhenNotApplicable()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var isolated = SeedIsolatedDepartment(harness);

        var missing = await harness.CreateMovement.ExecuteAsync(
            DepartmentChange(hired.EmploymentId, isolated, targetPositionId: null),
            CancellationToken.None);
        Assert.False(missing.IsSuccess);
        Assert.Equal(MovementValidation.Codes.TargetPositionRequired, missing.Error!.Code);

        var withPosition = await harness.CreateMovement.ExecuteAsync(
            DepartmentChange(hired.EmploymentId, isolated, harness.OtherPositionId),
            CancellationToken.None);
        Assert.True(withPosition.IsSuccess, withPosition.Error?.Detail);
    }

    [Fact]
    public async Task PositionChange_SamePosition_IsRejected()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var result = await harness.CreateMovement.ExecuteAsync(
            PositionChange(hired.EmploymentId, harness.PositionId),
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(MovementValidation.Codes.SameTarget, result.Error!.Code);
    }

    [Fact]
    public async Task Promotion_RequiresDifferentPosition_AndKeepsDepartment()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        harness.AddApplicability(harness.DepartmentId, harness.OtherPositionId);

        var same = await harness.CreateMovement.ExecuteAsync(
            Promotion(hired.EmploymentId, harness.PositionId),
            CancellationToken.None);
        Assert.False(same.IsSuccess);
        Assert.Equal(MovementValidation.Codes.SameTarget, same.Error!.Code);

        var promoted = await harness.CreateMovement.ExecuteAsync(
            Promotion(hired.EmploymentId, harness.OtherPositionId),
            CancellationToken.None);
        Assert.True(promoted.IsSuccess, promoted.Error?.Detail);
        Assert.Equal(PersonnelMovementType.Promotion, promoted.Value!.Type);
        Assert.Equal(harness.DepartmentId, promoted.Value.NewAssignment!.DepartmentId);
        Assert.Equal(harness.OtherPositionId, promoted.Value.NewAssignment.PositionId);
        Assert.Equal(2, harness.Store.Assignments.Count);
    }

    [Fact]
    public async Task PropertyTransfer_SameOrganization_Succeeds_CrossOrgRejected()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var transferred = await harness.CreateMovement.ExecuteAsync(
            PropertyTransfer(hired.EmploymentId, harness),
            CancellationToken.None);
        Assert.True(transferred.IsSuccess, transferred.Error?.Detail);
        Assert.Equal(PersonnelMovementType.PropertyTransfer, transferred.Value!.Type);
        Assert.Equal(hired.EmploymentId, transferred.Value.EmploymentId);
        Assert.Equal(harness.OtherPropertyId, transferred.Value.NewAssignment!.PropertyId);

        var foreignOrg = Guid.CreateVersion7();
        var foreignProperty = Guid.CreateVersion7();
        harness.Store.Organizations.Add(new Organization(foreignOrg, "Foreign"));
        harness.Store.Properties.Add(new Property(foreignProperty, foreignOrg, "Foreign Hotel", "UTC"));
        var cross = await harness.CreateMovement.ExecuteAsync(
            PropertyTransfer(hired.EmploymentId, harness) with { TargetPropertyId = foreignProperty },
            CancellationToken.None);
        Assert.False(cross.IsSuccess);
        Assert.Equal(MovementValidation.Codes.CrossOrganizationNotSupported, cross.Error!.Code);
    }

    [Fact]
    public async Task PropertyTransfer_OrgWideAccess_SucceedsWhenWorkplaceHasProperty()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var result = await harness.CreateMovement.ExecuteAsync(
            PropertyTransfer(hired.EmploymentId, harness) with { AccessiblePropertyIds = null },
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(PersonnelMovementType.PropertyTransfer, result.Value!.Type);
    }

    [Fact]
    public async Task PropertyTransfer_WithoutWorkplaceProperty_ReturnsPropertyContextRequired()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var useCase = new CreateWorkforceMovementUseCase(
            harness.Store,
            harness.Clock,
            new FixedWorkplace(harness.OrganizationId, Guid.Empty));
        var result = await useCase.ExecuteAsync(
            PropertyTransfer(hired.EmploymentId, harness) with { AccessiblePropertyIds = null },
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("property-context-required", result.Error!.Code);
    }

    [Fact]
    public async Task PropertyTransfer_RequiresSourceAndDestinationAccess()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var sourceOnly = await harness.CreateMovement.ExecuteAsync(
            PropertyTransfer(hired.EmploymentId, harness) with
            {
                AccessiblePropertyIds = new HashSet<Guid> { harness.PropertyId }
            },
            CancellationToken.None);
        Assert.False(sourceOnly.IsSuccess);
        Assert.Equal(MovementValidation.Codes.PropertyAccessDenied, sourceOnly.Error!.Code);

        var destOnly = await harness.CreateMovement.ExecuteAsync(
            PropertyTransfer(hired.EmploymentId, harness) with
            {
                AccessiblePropertyIds = new HashSet<Guid> { harness.OtherPropertyId }
            },
            CancellationToken.None);
        Assert.False(destOnly.IsSuccess);
        Assert.Equal(MovementValidation.Codes.PropertyAccessDenied, destOnly.Error!.Code);

        var both = await harness.CreateMovement.ExecuteAsync(
            PropertyTransfer(hired.EmploymentId, harness) with
            {
                AccessiblePropertyIds = new HashSet<Guid> { harness.PropertyId, harness.OtherPropertyId }
            },
            CancellationToken.None);
        Assert.True(both.IsSuccess, both.Error?.Detail);
    }

    [Fact]
    public async Task ManagerChange_HistoryAndCycles()
    {
        var harness = new WorkforceHarness();
        var subordinate = await HirePastAsync(harness);
        var managerA = await HireNamedAsync(harness, "Ali", "Manager");
        var managerB = await HireNamedAsync(harness, "Bora", "Boss");
        var managerC = await HireNamedAsync(harness, "Cem", "Chief");

        var initial = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(subordinate.EmploymentId, managerA.EmploymentId),
            CancellationToken.None);
        Assert.True(initial.IsSuccess, initial.Error?.Detail);

        var asOfHire = await ReportingLineResolver.ForEmploymentOnAsync(
            harness.Store, subordinate.EmploymentId, harness.Clock.Today.AddDays(-1), CancellationToken.None);
        Assert.Null(asOfHire);

        var todayLine = await ReportingLineResolver.ForEmploymentOnAsync(
            harness.Store, subordinate.EmploymentId, harness.Clock.Today, CancellationToken.None);
        Assert.Equal(managerA.EmploymentId, todayLine!.ManagerEmploymentId);

        var changed = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(subordinate.EmploymentId, managerB.EmploymentId, harness.Clock.Today.AddDays(2)),
            CancellationToken.None);
        Assert.True(changed.IsSuccess, changed.Error?.Detail);
        Assert.Equal(harness.Clock.Today.AddDays(1), harness.Store.ReportingLines.Single(item => item.Id == todayLine.Id).EffectiveTo);

        var self = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(managerB.EmploymentId, managerB.EmploymentId),
            CancellationToken.None);
        Assert.Equal(MovementValidation.Codes.SelfManager, self.Error!.Code);

        Assert.True((await harness.CreateMovement.ExecuteAsync(
            ManagerChange(managerA.EmploymentId, managerB.EmploymentId),
            CancellationToken.None)).IsSuccess);
        Assert.True((await harness.CreateMovement.ExecuteAsync(
            ManagerChange(managerB.EmploymentId, managerC.EmploymentId),
            CancellationToken.None)).IsSuccess);

        var twoNode = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(managerB.EmploymentId, managerA.EmploymentId, harness.Clock.Today.AddDays(5)),
            CancellationToken.None);
        Assert.Equal(MovementValidation.Codes.Cycle, twoNode.Error!.Code);

        var threeNode = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(managerC.EmploymentId, managerA.EmploymentId, harness.Clock.Today.AddDays(6)),
            CancellationToken.None);
        Assert.Equal(MovementValidation.Codes.Cycle, threeNode.Error!.Code);
    }

    [Fact]
    public async Task FutureMovement_DoesNotChangeCurrentAssignment_AndCancelRestores()
    {
        var harness = new WorkforceHarness();
        SetClock(harness, new DateOnly(2026, 9, 3));
        var hired = await HirePastAsync(harness, new DateOnly(2026, 8, 1));
        var original = harness.Store.Assignments.Single();

        var scheduled = await harness.CreateMovement.ExecuteAsync(
            DepartmentChange(hired.EmploymentId, harness.OtherDepartmentId, null, new DateOnly(2026, 9, 15)),
            CancellationToken.None);
        Assert.True(scheduled.IsSuccess, scheduled.Error?.Detail);
        Assert.Equal(PersonnelMovementLifecycle.Scheduled, scheduled.Value!.Lifecycle);

        var directory = await harness.HrDirectory.ExecuteAsync(canReadSensitive: false, CancellationToken.None);
        Assert.True(directory.IsSuccess, directory.Error?.Detail);
        var row = Assert.Single(directory.Value!, item => item.EmployeeId == hired.EmployeeId);
        Assert.Equal(harness.DepartmentId, row.DepartmentId);

        Assert.Equal(original.Id, PrimaryAssignments.Covering(harness.Store.Assignments, new DateOnly(2026, 9, 14))!.Id);
        Assert.Equal(
            scheduled.Value.NewAssignment!.Id,
            PrimaryAssignments.Covering(harness.Store.Assignments, new DateOnly(2026, 9, 15))!.Id);

        SetClock(harness, new DateOnly(2026, 9, 10));
        var cancelled = await harness.CancelMovement.ExecuteAsync(
            new CancelPersonnelMovementCommand(scheduled.Value.Id, "Plans changed", "hr-user", null),
            CancellationToken.None);
        Assert.True(cancelled.IsSuccess, cancelled.Error?.Detail);
        Assert.Equal(PersonnelMovementLifecycle.Cancelled, cancelled.Value!.Lifecycle);
        Assert.Null(original.EndDate);
        Assert.Single(harness.Store.Assignments);
        Assert.Null(harness.Store.PersonnelMovements.Single().NewAssignmentId);

        SetClock(harness, new DateOnly(2026, 9, 15));
        var tooLate = await harness.CancelMovement.ExecuteAsync(
            new CancelPersonnelMovementCommand(scheduled.Value.Id, "again", "hr-user", null),
            CancellationToken.None);
        Assert.Equal(MovementValidation.Codes.AlreadyCancelled, tooLate.Error!.Code);

        var again = await harness.CreateMovement.ExecuteAsync(
            DepartmentChange(hired.EmploymentId, harness.OtherDepartmentId, null, new DateOnly(2026, 9, 15)),
            CancellationToken.None);
        Assert.True(again.IsSuccess, again.Error?.Detail);
        var effectiveCancel = await harness.CancelMovement.ExecuteAsync(
            new CancelPersonnelMovementCommand(again.Value!.Id, "too late", "hr-user", null),
            CancellationToken.None);
        Assert.Equal(MovementValidation.Codes.AlreadyEffective, effectiveCancel.Error!.Code);
    }

    [Fact]
    public async Task PendingLeaveCrossingEffectiveDate_BlocksAssignmentMovement_NotManagerChange()
    {
        var harness = new WorkforceHarness();
        SetClock(harness, new DateOnly(2026, 9, 3));
        var hired = await HirePastAsync(harness, new DateOnly(2026, 8, 1));
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var leave = await harness.CreateLeaveRequest.ExecuteAsync(
            new CreateLeaveRequestCommand(
                hired.EmployeeId,
                hired.EmploymentId,
                leaveType.Id,
                new DateOnly(2026, 9, 10),
                new DateOnly(2026, 9, 20),
                11.0m,
                null,
                "actor"),
            CancellationToken.None);
        Assert.True(leave.IsSuccess, leave.Error?.Detail);

        var blocked = await harness.CreateMovement.ExecuteAsync(
            DepartmentChange(hired.EmploymentId, harness.OtherDepartmentId, null, new DateOnly(2026, 9, 15)),
            CancellationToken.None);
        Assert.Equal(MovementValidation.Codes.PendingLeaveConflict, blocked.Error!.Code);

        var outside = await harness.CreateMovement.ExecuteAsync(
            DepartmentChange(hired.EmploymentId, harness.OtherDepartmentId, null, new DateOnly(2026, 9, 25)),
            CancellationToken.None);
        Assert.True(outside.IsSuccess, outside.Error?.Detail);

        var manager = await HireNamedAsync(harness, "Yönetici", "Kişi");
        var managerMove = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(hired.EmploymentId, manager.EmploymentId, new DateOnly(2026, 9, 15)),
            CancellationToken.None);
        Assert.True(managerMove.IsSuccess, managerMove.Error?.Detail);
    }

    [Fact]
    public async Task FutureScheduleOnOldAssignment_BlocksMovement()
    {
        var harness = new WorkforceHarness();
        SetClock(harness, new DateOnly(2026, 9, 3));
        var hired = await HirePastAsync(harness, new DateOnly(2026, 8, 1));
        var shift = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Day", new TimeOnly(8, 0), new TimeOnly(16, 0), false, 30, "actor"),
            CancellationToken.None);
        Assert.True(shift.IsSuccess, shift.Error?.Detail);
        var scheduled = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                hired.EmployeeId,
                new DateOnly(2026, 9, 20),
                ScheduleEntryKind.Shift,
                shift.Value!.Id,
                null,
                "actor",
                harness.PropertyId),
            CancellationToken.None);
        Assert.True(scheduled.IsSuccess, scheduled.Error?.Detail);

        var blocked = await harness.CreateMovement.ExecuteAsync(
            DepartmentChange(hired.EmploymentId, harness.OtherDepartmentId, null, new DateOnly(2026, 9, 15)),
            CancellationToken.None);
        Assert.Equal(MovementValidation.Codes.ScheduleConflict, blocked.Error!.Code);
    }

    [Fact]
    public async Task Puantaj_UsesDatedAssignment_NotLatestRow()
    {
        var harness = new WorkforceHarness();
        SetClock(harness, new DateOnly(2026, 9, 3));
        var hired = await HirePastAsync(harness, new DateOnly(2026, 8, 1));
        var moved = await harness.CreateMovement.ExecuteAsync(
            DepartmentChange(hired.EmploymentId, harness.OtherDepartmentId, null, new DateOnly(2026, 9, 15)),
            CancellationToken.None);
        Assert.True(moved.IsSuccess, moved.Error?.Detail);

        var month = await harness.GetAttendanceMonth.ExecuteAsync(
            2026,
            9,
            departmentId: null,
            search: null,
            scopedPropertyId: harness.PropertyId,
            allowedDepartmentIds: null,
            CancellationToken.None);
        Assert.True(month.IsSuccess, month.Error?.Detail);
        var row = Assert.Single(month.Value!.Employees, item => item.EmployeeId == hired.EmployeeId);
        Assert.Equal(harness.DepartmentId, row.Days.Single(item => item.LocalDate == new DateOnly(2026, 9, 14)).DepartmentId);
        Assert.Equal(harness.OtherDepartmentId, row.Days.Single(item => item.LocalDate == new DateOnly(2026, 9, 15)).DepartmentId);
        Assert.Equal(moved.Value!.PreviousAssignment!.Id, row.Days.Single(item => item.LocalDate == new DateOnly(2026, 9, 14)).AssignmentId);
        Assert.Equal(moved.Value.NewAssignment!.Id, row.Days.Single(item => item.LocalDate == new DateOnly(2026, 9, 15)).AssignmentId);
    }

    [Fact]
    public async Task List_RespectsPropertyScope()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        Assert.True((await harness.CreateMovement.ExecuteAsync(
            DepartmentChange(hired.EmploymentId, harness.OtherDepartmentId, null),
            CancellationToken.None)).IsSuccess);

        var hidden = await harness.ListMovements.ExecuteAsync(
            new ListPersonnelMovementsFilter(null, null, null, null, null, null, new HashSet<Guid> { harness.OtherPropertyId }),
            CancellationToken.None);
        Assert.Empty(hidden.Value!);

        var visible = await harness.ListMovements.ExecuteAsync(
            new ListPersonnelMovementsFilter(null, null, null, null, hired.EmployeeId, null, null),
            CancellationToken.None);
        Assert.Single(visible.Value!);
    }

    private static CreatePersonnelMovementCommand DepartmentChange(
        Guid employmentId,
        Guid departmentId,
        Guid? targetPositionId,
        DateOnly? effective = null) =>
        new(
            null,
            employmentId,
            PersonnelMovementType.DepartmentChange,
            effective ?? new DateOnly(2026, 8, 21),
            null,
            departmentId,
            targetPositionId,
            null,
            false,
            "Department reorganization",
            null,
            "hr-user",
            null);

    private static CreatePersonnelMovementCommand PositionChange(Guid employmentId, Guid positionId) =>
        new(
            null,
            employmentId,
            PersonnelMovementType.PositionChange,
            new DateOnly(2026, 8, 21),
            null,
            null,
            positionId,
            null,
            false,
            "Role change",
            null,
            "hr-user",
            null);

    private static CreatePersonnelMovementCommand Promotion(Guid employmentId, Guid positionId) =>
        new(
            null,
            employmentId,
            PersonnelMovementType.Promotion,
            new DateOnly(2026, 8, 21),
            null,
            null,
            positionId,
            null,
            false,
            "Promotion",
            null,
            "hr-user",
            null);

    private static CreatePersonnelMovementCommand PropertyTransfer(Guid employmentId, WorkforceHarness harness) =>
        new(
            null,
            employmentId,
            PersonnelMovementType.PropertyTransfer,
            harness.Clock.Today,
            harness.OtherPropertyId,
            harness.OtherPropertyDepartmentId,
            harness.OtherPropertyPositionId,
            null,
            false,
            "Property transfer",
            null,
            "hr-user",
            null);

    private static CreatePersonnelMovementCommand ManagerChange(
        Guid employmentId,
        Guid managerEmploymentId,
        DateOnly? effective = null) =>
        new(
            null,
            employmentId,
            PersonnelMovementType.ManagerChange,
            effective ?? new DateOnly(2026, 8, 21),
            null,
            null,
            null,
            managerEmploymentId,
            false,
            "Reporting line",
            null,
            "hr-user",
            null);

    private static async Task<(Guid EmployeeId, Guid EmploymentId)> HirePastAsync(
        WorkforceHarness harness,
        DateOnly? start = null)
    {
        var hired = await harness.Hire.ExecuteAsync(
            harness.HireCommand(startDate: start ?? harness.Clock.Today.AddDays(-10)),
            CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        return (hired.Value!.EmployeeId, hired.Value.EmploymentId);
    }

    private static async Task<(Guid EmployeeId, Guid EmploymentId)> HireNamedAsync(
        WorkforceHarness harness,
        string given,
        string family)
    {
        var hired = await harness.Hire.ExecuteAsync(
            new HireEmployeeCommand(given, family, harness.Clock.Today.AddDays(-10), harness.DepartmentId, harness.PositionId),
            CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        return (hired.Value!.EmployeeId, hired.Value.EmploymentId);
    }

    private static Guid SeedIsolatedDepartment(WorkforceHarness harness)
    {
        var id = Guid.CreateVersion7();
        Assert.True(Department.TryCreate(id, harness.PropertyId, "Satış", null, out var department, out _));
        harness.Store.Departments.Add(department!);
        harness.AddApplicability(id, harness.OtherPositionId);
        return id;
    }

    private static void SetClock(WorkforceHarness harness, DateOnly day)
    {
        harness.Clock.Today = day;
        harness.Clock.UtcNow = new DateTimeOffset(day.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero);
    }
}
