using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class WorkforceManagerHierarchyTests
{
    [Fact]
    public void RequiredManagerLevel_IsMinimumConfiguredLevelAboveSubordinate()
    {
        var positions = new[]
        {
            PositionAt(100),
            PositionAt(150),
            PositionAt(200),
            PositionAt(300),
        };

        Assert.Equal(150, ManagerHierarchy.RequiredManagerLevel(positions, 100));
        Assert.Equal(200, ManagerHierarchy.RequiredManagerLevel(positions, 150));
        Assert.Null(ManagerHierarchy.RequiredManagerLevel(positions, 300));
        Assert.Equal(200, ManagerHierarchy.RequiredManagerLevel([PositionAt(100, active: false), PositionAt(200)], 100));
        Assert.True(PromotionHierarchy.IsHigherLevel(100, 200));
        Assert.True(PromotionHierarchy.IsHigherLevel(100, 300));
        Assert.False(PromotionHierarchy.IsHigherLevel(200, 200));
        Assert.False(PromotionHierarchy.IsHigherLevel(200, 100));
    }

    [Fact]
    public async Task Employee100_Manager200_CanManage_Succeeds()
    {
        var harness = new WorkforceHarness();
        var employee = await HireAtAsync(harness, harness.PositionId, "Ayşe", "Yılmaz");
        var manager = await HireAtAsync(harness, harness.Level200PositionId, "Ali", "Şef");

        var result = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(employee.EmploymentId, manager.EmploymentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
    }

    [Fact]
    public async Task Employee100_Manager300_Fails()
    {
        await AssertManagerRejectedAsync(harness => (harness.PositionId, harness.Level300PositionId));
    }

    [Fact]
    public async Task Employee100_Manager400_Fails()
    {
        await AssertManagerRejectedAsync(harness => (harness.PositionId, harness.Level400PositionId));
    }

    [Fact]
    public async Task Employee200_Manager300_Succeeds()
    {
        var harness = new WorkforceHarness();
        var employee = await HireAtAsync(harness, harness.Level200PositionId, "Ayşe", "Şef");
        var manager = await HireAtAsync(harness, harness.Level300PositionId, "Ali", "Müdür");

        var result = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(employee.EmploymentId, manager.EmploymentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
    }

    [Fact]
    public async Task Employee200_Manager400_Fails()
    {
        await AssertManagerRejectedAsync(harness => (harness.Level200PositionId, harness.Level400PositionId));
    }

    [Fact]
    public async Task Employee100_Candidate200_CannotManage_Fails()
    {
        var harness = new WorkforceHarness();
        var specialistId = Guid.CreateVersion7();
        Assert.True(Position.TryCreate(
            specialistId,
            harness.PropertyId,
            "Kıdemli Uzman",
            null,
            200,
            false,
            out var specialist,
            out _));
        harness.Store.Positions.Add(specialist!);
        harness.AddApplicability(harness.DepartmentId, specialistId);

        var employee = await HireAtAsync(harness, harness.PositionId, "Ayşe", "Yılmaz");
        var candidate = await HireAtAsync(harness, specialistId, "Burak", "Uzman");

        var result = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(employee.EmploymentId, candidate.EmploymentId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MovementValidation.Codes.ManagerCannotManage, result.Error!.Code);
    }

    [Fact]
    public async Task NoEligibleManagerAtNextLevel_DoesNotFallThrough_AndOmitsHigherManagers()
    {
        var harness = new WorkforceHarness();
        var employee = await HireAtAsync(harness, harness.PositionId, "Ayşe", "Yılmaz");
        var skipped = await HireAtAsync(harness, harness.Level300PositionId, "Cem", "Müdür");

        var listed = await harness.ListManagerCandidates.ExecuteAsync(
            employee.EmploymentId,
            harness.Clock.Today,
            null,
            CancellationToken.None);

        Assert.True(listed.IsSuccess, listed.Error?.Detail);
        Assert.Empty(listed.Value!);

        var save = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(employee.EmploymentId, skipped.EmploymentId),
            CancellationToken.None);
        Assert.Equal(MovementValidation.Codes.ManagerLevelInvalid, save.Error!.Code);
    }

    [Fact]
    public async Task IntermediaryConfiguredLevel_BecomesRequiredManagerLevel()
    {
        var harness = new WorkforceHarness();
        var level150 = Guid.CreateVersion7();
        Assert.True(Position.TryCreate(level150, harness.PropertyId, "Grup Şefi", null, 150, true, out var position, out _));
        harness.Store.Positions.Add(position!);
        harness.AddApplicability(harness.DepartmentId, level150);

        var employee = await HireAtAsync(harness, harness.PositionId, "Ayşe", "Yılmaz");
        var next = await HireAtAsync(harness, level150, "Dilek", "Grup");
        var skipped = await HireAtAsync(harness, harness.Level200PositionId, "Emre", "Şef");

        var pass = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(employee.EmploymentId, next.EmploymentId),
            CancellationToken.None);
        Assert.True(pass.IsSuccess, pass.Error?.Detail);

        var fail = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(employee.EmploymentId, skipped.EmploymentId, harness.Clock.Today.AddDays(1)),
            CancellationToken.None);
        Assert.Equal(MovementValidation.Codes.ManagerLevelInvalid, fail.Error!.Code);

        var listed = await harness.ListManagerCandidates.ExecuteAsync(
            employee.EmploymentId,
            harness.Clock.Today.AddDays(1),
            null,
            CancellationToken.None);
        Assert.DoesNotContain(listed.Value!, item => item.EmploymentId == skipped.EmploymentId);
        Assert.Contains(listed.Value!, item => item.EmploymentId == next.EmploymentId);
    }

    [Fact]
    public async Task FutureDatedAssignment_ResolvesNextLevelAsOfEffectiveDate()
    {
        var harness = new WorkforceHarness();
        var employee = await HireAtAsync(harness, harness.PositionId, "Ayşe", "Yılmaz");
        var manager200 = await HireAtAsync(harness, harness.Level200PositionId, "Ali", "Şef");
        var manager300 = await HireAtAsync(harness, harness.Level300PositionId, "Bora", "Müdür");
        var future = harness.Clock.Today.AddDays(10);

        Assert.True((await harness.CreateMovement.ExecuteAsync(
            new CreatePersonnelMovementCommand(
                null,
                employee.EmploymentId,
                PersonnelMovementType.Promotion,
                future,
                null,
                null,
                harness.Level200PositionId,
                null,
                false,
                "Future promotion",
                null,
                "hr-user",
                null),
            CancellationToken.None)).IsSuccess);

        var todayCandidates = await harness.ListManagerCandidates.ExecuteAsync(
            employee.EmploymentId,
            harness.Clock.Today,
            null,
            CancellationToken.None);
        Assert.Contains(todayCandidates.Value!, item => item.EmploymentId == manager200.EmploymentId);
        Assert.DoesNotContain(todayCandidates.Value!, item => item.EmploymentId == manager300.EmploymentId);

        var futureCandidates = await harness.ListManagerCandidates.ExecuteAsync(
            employee.EmploymentId,
            future,
            null,
            CancellationToken.None);
        Assert.DoesNotContain(futureCandidates.Value!, item => item.EmploymentId == manager200.EmploymentId);
        Assert.Contains(futureCandidates.Value!, item => item.EmploymentId == manager300.EmploymentId);

        var futureSave = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(employee.EmploymentId, manager200.EmploymentId, future),
            CancellationToken.None);
        Assert.Equal(MovementValidation.Codes.ManagerLevelInvalid, futureSave.Error!.Code);

        var futureOk = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(employee.EmploymentId, manager300.EmploymentId, future),
            CancellationToken.None);
        Assert.True(futureOk.IsSuccess, futureOk.Error?.Detail);
    }

    [Fact]
    public async Task CrossPropertyManager_AtExactNextLevel_IsAllowed()
    {
        var harness = new WorkforceHarness();
        var employee = await HireAtAsync(harness, harness.PositionId, "Ayşe", "Yılmaz");
        var manager = await HireAtAsync(harness, harness.Level200PositionId, "Deniz", "Antalya");
        Assert.True((await harness.CreateMovement.ExecuteAsync(
            new CreatePersonnelMovementCommand(
                null,
                manager.EmploymentId,
                PersonnelMovementType.PropertyTransfer,
                harness.Clock.Today,
                harness.OtherPropertyId,
                harness.OtherPropertyDepartmentId,
                harness.OtherPropertyPositionId,
                null,
                false,
                "Move manager",
                null,
                "hr-user",
                new HashSet<Guid> { harness.PropertyId, harness.OtherPropertyId }),
            CancellationToken.None)).IsSuccess);

        var result = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(employee.EmploymentId, manager.EmploymentId, harness.Clock.Today.AddDays(1)),
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
    }

    [Fact]
    public async Task Promotion_MaySkipLevels()
    {
        var harness = new WorkforceHarness();
        var employee = await HireAtAsync(harness, harness.PositionId, "Ayşe", "Yılmaz");
        harness.AddApplicability(harness.DepartmentId, harness.Level300PositionId);

        var promoted = await harness.CreateMovement.ExecuteAsync(
            new CreatePersonnelMovementCommand(
                null,
                employee.EmploymentId,
                PersonnelMovementType.Promotion,
                harness.Clock.Today,
                null,
                null,
                harness.Level300PositionId,
                null,
                false,
                "Skip-level promotion",
                null,
                "hr-user",
                null),
            CancellationToken.None);

        Assert.True(promoted.IsSuccess, promoted.Error?.Detail);
        Assert.Equal(PersonnelMovementType.Promotion, promoted.Value!.Type);
        Assert.Equal(harness.Level300PositionId, promoted.Value.NewAssignment!.PositionId);
    }

    [Fact]
    public async Task Promotion_100_To_200_Succeeds()
    {
        var harness = new WorkforceHarness();
        var employee = await HireAtAsync(harness, harness.PositionId, "Ayşe", "Yılmaz");

        var promoted = await harness.CreateMovement.ExecuteAsync(
            PromotionTo(employee.EmploymentId, harness.Level200PositionId, harness.Clock.Today),
            CancellationToken.None);

        Assert.True(promoted.IsSuccess, promoted.Error?.Detail);
        Assert.Equal(harness.Level200PositionId, promoted.Value!.NewAssignment!.PositionId);
    }

    [Fact]
    public async Task Promotion_200_To_300_Succeeds()
    {
        var harness = new WorkforceHarness();
        var employee = await HireAtAsync(harness, harness.Level200PositionId, "Elif", "Şahin");

        var promoted = await harness.CreateMovement.ExecuteAsync(
            PromotionTo(employee.EmploymentId, harness.Level300PositionId, harness.Clock.Today),
            CancellationToken.None);

        Assert.True(promoted.IsSuccess, promoted.Error?.Detail);
        Assert.Equal(harness.Level300PositionId, promoted.Value!.NewAssignment!.PositionId);
    }

    [Fact]
    public async Task Promotion_Equal_Or_Lower_Level_Fails()
    {
        var harness = new WorkforceHarness();
        harness.AddApplicability(harness.DepartmentId, harness.OtherPositionId);
        var employee = await HireAtAsync(harness, harness.Level200PositionId, "Elif", "Şahin");

        var equal = await harness.CreateMovement.ExecuteAsync(
            PromotionTo(employee.EmploymentId, harness.OtherPositionId, harness.Clock.Today),
            CancellationToken.None);
        Assert.False(equal.IsSuccess);
        Assert.Equal(MovementValidation.Codes.TargetNotPromotion, equal.Error!.Code);

        var lower = await harness.CreateMovement.ExecuteAsync(
            PromotionTo(employee.EmploymentId, harness.PositionId, harness.Clock.Today),
            CancellationToken.None);
        Assert.False(lower.IsSuccess);
        Assert.Equal(MovementValidation.Codes.TargetNotPromotion, lower.Error!.Code);

        var same = await harness.CreateMovement.ExecuteAsync(
            PromotionTo(employee.EmploymentId, harness.Level200PositionId, harness.Clock.Today),
            CancellationToken.None);
        Assert.False(same.IsSuccess);
        Assert.Equal(MovementValidation.Codes.SameTarget, same.Error!.Code);
    }

    [Fact]
    public async Task Promotion_FutureDate_Uses_Source_Position_As_Of_EffectiveDate()
    {
        var harness = new WorkforceHarness();
        harness.AddApplicability(harness.DepartmentId, harness.OtherPositionId);
        var employee = await HireAtAsync(harness, harness.PositionId, "Elif", "Şahin");
        var future = harness.Clock.Today.AddDays(5);
        var afterMove = future.AddDays(1);

        var moved = await harness.CreateMovement.ExecuteAsync(
            new CreatePersonnelMovementCommand(
                null,
                employee.EmploymentId,
                PersonnelMovementType.PositionChange,
                future,
                null,
                null,
                harness.Level200PositionId,
                null,
                false,
                "Scheduled grade change",
                null,
                "hr-user",
                null),
            CancellationToken.None);
        Assert.True(moved.IsSuccess, moved.Error?.Detail);

        var demote = await harness.CreateMovement.ExecuteAsync(
            PromotionTo(employee.EmploymentId, harness.OtherPositionId, afterMove),
            CancellationToken.None);
        Assert.False(demote.IsSuccess);
        Assert.Equal(MovementValidation.Codes.TargetNotPromotion, demote.Error!.Code);

        var skip = await harness.CreateMovement.ExecuteAsync(
            PromotionTo(employee.EmploymentId, harness.Level300PositionId, afterMove),
            CancellationToken.None);
        Assert.True(skip.IsSuccess, skip.Error?.Detail);
    }

    [Fact]
    public async Task PositionChange_May_Move_To_Equal_Or_Lower_Level()
    {
        var harness = new WorkforceHarness();
        var employee = await HireAtAsync(harness, harness.Level200PositionId, "Elif", "Şahin");

        var lower = await harness.CreateMovement.ExecuteAsync(
            new CreatePersonnelMovementCommand(
                null,
                employee.EmploymentId,
                PersonnelMovementType.PositionChange,
                harness.Clock.Today,
                null,
                null,
                harness.PositionId,
                null,
                false,
                "Role change",
                null,
                "hr-user",
                null),
            CancellationToken.None);
        Assert.True(lower.IsSuccess, lower.Error?.Detail);
        Assert.Equal(PersonnelMovementType.PositionChange, lower.Value!.Type);
        Assert.Equal(harness.PositionId, lower.Value.NewAssignment!.PositionId);
    }

    private static CreatePersonnelMovementCommand PromotionTo(
        Guid employmentId,
        Guid positionId,
        DateOnly effectiveDate) =>
        new(
            null,
            employmentId,
            PersonnelMovementType.Promotion,
            effectiveDate,
            null,
            null,
            positionId,
            null,
            false,
            "Promotion",
            null,
            "hr-user",
            null);

    private static async Task AssertManagerRejectedAsync(Func<WorkforceHarness, (Guid EmployeePosition, Guid ManagerPosition)> positions)
    {
        var harness = new WorkforceHarness();
        var (employeePosition, managerPosition) = positions(harness);
        var employee = await HireAtAsync(harness, employeePosition, "Ayşe", "Yılmaz");
        var manager = await HireAtAsync(harness, managerPosition, "Ali", "Yönetici");

        var result = await harness.CreateMovement.ExecuteAsync(
            ManagerChange(employee.EmploymentId, manager.EmploymentId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(MovementValidation.Codes.ManagerLevelInvalid, result.Error!.Code);
    }

    private static async Task<(Guid EmployeeId, Guid EmploymentId)> HireAtAsync(
        WorkforceHarness harness,
        Guid positionId,
        string given,
        string family)
    {
        var hired = await harness.Hire.ExecuteAsync(
            new HireEmployeeCommand(given, family, harness.Clock.Today.AddDays(-10), harness.DepartmentId, positionId),
            CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        return (hired.Value!.EmployeeId, hired.Value.EmploymentId);
    }

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

    private static Position PositionAt(int level, bool active = true, bool canManage = false)
    {
        Assert.True(Position.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            $"Level {level}",
            null,
            level,
            canManage,
            out var position,
            out _));
        if (!active)
        {
            position!.Deactivate();
        }

        return position!;
    }
}
