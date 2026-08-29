using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class WorkforceInvariantTests
{
    [Fact]
    public void PersonnelNumber_IsNotPrimaryKey()
    {
        Assert.Equal("Id", nameof(Employee.Id));
        Assert.NotEqual(nameof(Employee.Id), nameof(Employee.PersonnelNumber));
    }

    [Fact]
    public async Task PersonnelNumber_IsUniqueWithinOrganization()
    {
        var harness = new WorkforceHarness();
        var first = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        var second = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.NotEqual(first.Value!.PersonnelNumber, second.Value!.PersonnelNumber);
        Assert.Equal(
            2,
            harness.Store.Employees.Select(item => item.PersonnelNumber).Distinct().Count());
    }

    [Fact]
    public void EmploymentPeriod_CannotBeInverted()
    {
        var employment = Employment.Open(Guid.CreateVersion7(), Guid.CreateVersion7(), new DateOnly(2026, 8, 21), new DateOnly(2026, 8, 21));

        Assert.False(employment.TryEnd(new DateOnly(2026, 8, 20), EmploymentTerminationReason.Resignation, out _));
        Assert.False(employment.IsEnded);
        Assert.True(employment.TryEnd(new DateOnly(2026, 8, 21), EmploymentTerminationReason.Resignation, out _));
        Assert.True(employment.IsEnded);
        Assert.Equal(new DateOnly(2026, 8, 21), employment.EndDate);
    }

    [Fact]
    public void AssignmentPeriod_CannotBeInverted()
    {
        var assignment = Assignment.StartPrimary(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 8, 21));

        Assert.False(assignment.TryCloseOn(new DateOnly(2026, 8, 20), out _));
        Assert.Null(assignment.EndDate);
        Assert.True(assignment.TryCloseOn(new DateOnly(2026, 8, 21), out _));
        Assert.Equal(new DateOnly(2026, 8, 21), assignment.EndDate);
    }

    [Fact]
    public async Task Employee_CannotHaveMultipleNonEndedEmployments()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        var second = Employment.Open(
            Guid.CreateVersion7(),
            hired.Value!.EmployeeId,
            harness.Clock.Today,
            harness.Clock.Today);
        harness.Store.Employments.Add(second);

        var nonEnded = harness.Store.Employments.Count(item => !item.IsEnded && item.EmployeeId == hired.Value.EmployeeId);
        Assert.Equal(2, nonEnded);

        var ended = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(hired.Value.EmployeeId, harness.Clock.Today, EmploymentTerminationReason.Resignation),
            CancellationToken.None);

        Assert.False(ended.IsSuccess);
    }

    [Fact]
    public void PrimaryAssignments_CannotOverlap()
    {
        var employmentId = Guid.CreateVersion7();
        var first = Assignment.StartPrimary(
            Guid.CreateVersion7(),
            employmentId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 8, 1));
        Assert.True(first.TryCloseOn(new DateOnly(2026, 8, 10), out _));
        var second = Assignment.StartPrimary(
            Guid.CreateVersion7(),
            employmentId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 8, 10));

        Assert.True(PrimaryAssignments.HasOverlap([first, second]));

        var sequential = Assignment.StartPrimary(
            Guid.CreateVersion7(),
            employmentId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 8, 11));
        Assert.False(PrimaryAssignments.HasOverlap([first, sequential]));
    }

    [Fact]
    public async Task EndedEmployment_CannotReceiveNewAssignment()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        Assert.True((await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(hired.Value!.EmployeeId, harness.Clock.Today, EmploymentTerminationReason.Resignation),
            CancellationToken.None)).IsSuccess);

        var result = await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                hired.Value.EmployeeId,
                harness.OtherDepartmentId,
                harness.OtherPositionId,
                harness.Clock.Today.AddDays(1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("no-current-employment", result.Error!.Code);
    }

    [Fact]
    public async Task ActiveWorkforce_ReturnsOnlyPeopleWorkingToday()
    {
        var harness = new WorkforceHarness();
        var active = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        await harness.Hire.ExecuteAsync(
            harness.HireCommand(startDate: harness.Clock.Today.AddDays(3)),
            CancellationToken.None);
        var leaving = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(leaving.Value!.EmployeeId, harness.Clock.Today, EmploymentTerminationReason.Resignation),
            CancellationToken.None);

        var list = await harness.ActiveWorkforce.ExecuteAsync(CancellationToken.None);

        Assert.True(list.IsSuccess);
        Assert.Single(list.Value!);
        Assert.Equal(active.Value!.EmployeeId, list.Value[0].EmployeeId);
        Assert.Equal("Kat Hizmetleri", list.Value[0].DepartmentName);
        Assert.Equal("Kat Görevlisi", list.Value[0].PositionName);
    }

    [Fact]
    public void Position_ExistsIndependentlyOfDepartment()
    {
        var propertyId = Guid.CreateVersion7();

        Assert.True(Position.TryCreate(Guid.CreateVersion7(), propertyId, "Uzman", null, out var position, out _));
        Assert.NotNull(position);
        Assert.Equal(propertyId, position.PropertyId);
        Assert.Null(typeof(Position).GetProperty("DepartmentId"));
        Assert.Equal("PropertyId", nameof(Position.PropertyId));
        Assert.Equal("DepartmentId", nameof(Assignment.DepartmentId));
        Assert.Equal("PositionId", nameof(Assignment.PositionId));
    }

    [Fact]
    public async Task SamePosition_CanBeUsedByAssignmentsInDifferentDepartments()
    {
        var harness = new WorkforceHarness();
        var first = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: harness.DepartmentId, positionId: harness.PositionId),
            CancellationToken.None);
        var second = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: harness.OtherDepartmentId, positionId: harness.PositionId),
            CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error?.Detail);
        Assert.True(second.IsSuccess, second.Error?.Detail);
        Assert.Equal(harness.PositionId, first.Value!.PositionId);
        Assert.Equal(harness.PositionId, second.Value!.PositionId);
        Assert.Equal(harness.DepartmentId, first.Value.DepartmentId);
        Assert.Equal(harness.OtherDepartmentId, second.Value.DepartmentId);
        Assert.Equal(2, harness.Store.Assignments.Count(item => item.PositionId == harness.PositionId));
        Assert.Equal(2, harness.Store.Assignments.Select(item => item.DepartmentId).Distinct().Count());
    }

    [Fact]
    public void PositionAndDepartment_DoNotGrantPermissions()
    {
        foreach (var type in new[] { typeof(Position), typeof(Department) })
        {
            var names = type.GetProperties().Select(property => property.Name).ToArray();
            Assert.DoesNotContain("Permission", names);
            Assert.DoesNotContain("Permissions", names);
            Assert.DoesNotContain("Role", names);
            Assert.DoesNotContain("Roles", names);
            Assert.DoesNotContain("Claim", names);
            Assert.DoesNotContain("Claims", names);
        }

        Assert.DoesNotContain(
            typeof(Position).Assembly.GetReferencedAssemblies().Select(name => name.Name),
            name => name == "HuGuWeb.Api");
    }
}
