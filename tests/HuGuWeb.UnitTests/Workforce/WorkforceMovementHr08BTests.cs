using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class WorkforceMovementHr08BTests
{
    [Fact]
    public async Task List_FiltersByPropertyId_WithoutCrossingScope()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var created = await harness.CreateMovement.ExecuteAsync(
            new CreatePersonnelMovementCommand(
                null,
                hired.EmploymentId,
                PersonnelMovementType.DepartmentChange,
                harness.Clock.Today,
                null,
                harness.OtherDepartmentId,
                null,
                null,
                false,
                "Reorganization",
                null,
                "hr-user",
                null),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);

        var matched = await harness.ListMovements.ExecuteAsync(
            new ListPersonnelMovementsFilter(null, null, null, null, null, null, null, harness.PropertyId),
            CancellationToken.None);
        Assert.Single(matched.Value!);

        var otherProperty = await harness.ListMovements.ExecuteAsync(
            new ListPersonnelMovementsFilter(null, null, null, null, null, null, null, harness.OtherPropertyId),
            CancellationToken.None);
        Assert.Empty(otherProperty.Value!);

        var denied = await harness.ListMovements.ExecuteAsync(
            new ListPersonnelMovementsFilter(
                null,
                null,
                null,
                null,
                null,
                null,
                new HashSet<Guid> { harness.OtherPropertyId },
                harness.PropertyId),
            CancellationToken.None);
        Assert.Empty(denied.Value!);
    }

    [Fact]
    public void Actor_Display_Prefers_Person_Then_User_Then_Email_And_Never_A_Raw_Guid()
    {
        const string userId = "8528d29a-b042-4c3a-8dcf-b22255877825";
        var person = MovementActorNaming.Resolve(userId, "Ayşe Yılmaz", "Seed Admin", "admin@hugu.local");
        Assert.Equal(userId, person.Id);
        Assert.Equal("Ayşe Yılmaz", person.DisplayName);

        var applicationUser = MovementActorNaming.Resolve(userId, null, "Seed Admin", "admin@hugu.local");
        Assert.Equal("Seed Admin", applicationUser.DisplayName);

        var email = MovementActorNaming.Resolve(userId, null, userId, "admin@hugu.local");
        Assert.Equal("admin@hugu.local", email.DisplayName);

        var username = MovementActorNaming.Resolve(userId, null, "hr.admin", "admin@hugu.local");
        Assert.Equal("hr.admin", username.DisplayName);

        var unresolved = MovementActorNaming.Resolve(userId, userId, userId, userId);
        Assert.Equal(userId, unresolved.Id);
        Assert.Null(unresolved.DisplayName);
        Assert.True(MovementActorNaming.LooksLikeRawUserId(userId));

        var system = MovementActorNaming.Resolve("  ", null, null, null);
        Assert.Null(system.Id);
        Assert.Null(system.DisplayName);
    }

    [Fact]
    public async Task List_Includes_Actor_Dto_Without_Requiring_A_Migration()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var created = await harness.CreateMovement.ExecuteAsync(
            new CreatePersonnelMovementCommand(
                null,
                hired.EmploymentId,
                PersonnelMovementType.DepartmentChange,
                harness.Clock.Today,
                null,
                harness.OtherDepartmentId,
                null,
                null,
                false,
                "Reorganization",
                null,
                "hr-user",
                null),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        Assert.Equal("hr-user", created.Value!.Actor.Id);
        Assert.Null(created.Value.Actor.DisplayName);

        var listed = await harness.ListMovements.ExecuteAsync(
            new ListPersonnelMovementsFilter(null, null, null, null, null, null, null, harness.PropertyId),
            CancellationToken.None);
        var item = Assert.Single(listed.Value!);
        Assert.Equal("hr-user", item.CreatedByUserId);
        Assert.Equal("hr-user", item.Actor.Id);
        Assert.Null(item.Actor.DisplayName);
    }

    [Fact]
    public async Task Structure_ReturnsTargetPropertyDepartmentsAndPositions()
    {
        var harness = new WorkforceHarness();
        var allowed = await harness.ListMovementStructure.ExecuteAsync(
            harness.OtherPropertyId,
            new HashSet<Guid> { harness.PropertyId, harness.OtherPropertyId },
            CancellationToken.None);
        Assert.True(allowed.IsSuccess, allowed.Error?.Detail);
        Assert.Equal(harness.OtherPropertyId, allowed.Value!.PropertyId);
        Assert.Contains(allowed.Value.Departments, item => item.Id == harness.OtherPropertyDepartmentId);
        Assert.Contains(allowed.Value.Positions, item => item.Id == harness.OtherPropertyPositionId);

        var denied = await harness.ListMovementStructure.ExecuteAsync(
            harness.OtherPropertyId,
            new HashSet<Guid> { harness.PropertyId },
            CancellationToken.None);
        Assert.False(denied.IsSuccess);
        Assert.Equal("movement-property-access-denied", denied.Error!.Code);
    }

    [Fact]
    public async Task PropertyTransfer_SourceOnlyScope_CannotUseUnauthorizedDestination()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var result = await harness.CreateMovement.ExecuteAsync(
            PropertyTransfer(hired.EmploymentId, harness) with
            {
                AccessiblePropertyIds = new HashSet<Guid> { harness.PropertyId }
            },
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(MovementValidation.Codes.PropertyAccessDenied, result.Error!.Code);
    }

    [Fact]
    public async Task PropertyTransfer_OrgWideScope_CanTransferToAnotherProperty()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var result = await harness.CreateMovement.ExecuteAsync(
            PropertyTransfer(hired.EmploymentId, harness) with { AccessiblePropertyIds = null },
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(harness.OtherPropertyId, result.Value!.NewAssignment!.PropertyId);
    }

    [Fact]
    public async Task PropertyTransfer_SameProperty_IsRejected()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var result = await harness.CreateMovement.ExecuteAsync(
            PropertyTransfer(hired.EmploymentId, harness) with
            {
                TargetPropertyId = harness.PropertyId,
                TargetDepartmentId = harness.OtherDepartmentId,
                TargetPositionId = harness.OtherPositionId
            },
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(MovementValidation.Codes.SameTarget, result.Error!.Code);
    }

    [Fact]
    public async Task PropertyTransfer_CrossOrganization_IsRejected()
    {
        var harness = new WorkforceHarness();
        var hired = await HirePastAsync(harness);
        var foreignOrg = Guid.CreateVersion7();
        var foreignProperty = Guid.CreateVersion7();
        harness.Store.Organizations.Add(new Organization(foreignOrg, "Foreign"));
        harness.Store.Properties.Add(new Property(foreignProperty, foreignOrg, "Foreign Hotel", "UTC"));
        var result = await harness.CreateMovement.ExecuteAsync(
            PropertyTransfer(hired.EmploymentId, harness) with { TargetPropertyId = foreignProperty },
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(MovementValidation.Codes.CrossOrganizationNotSupported, result.Error!.Code);
    }

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

    private static async Task<(Guid EmployeeId, Guid EmploymentId)> HirePastAsync(WorkforceHarness harness)
    {
        var hired = await harness.Hire.ExecuteAsync(
            harness.HireCommand(startDate: harness.Clock.Today.AddDays(-10)),
            CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        return (hired.Value!.EmployeeId, hired.Value.EmploymentId);
    }
}
