using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed record AssignmentSummaryDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    Guid PositionId,
    string PositionName,
    Guid PropertyId,
    string PropertyName,
    DateOnly StartDate,
    DateOnly? EndDate);

public sealed record ReportingLineSummaryDto(
    Guid Id,
    Guid ManagerEmploymentId,
    Guid ManagerEmployeeId,
    string ManagerGivenName,
    string ManagerFamilyName,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);

public sealed record PersonnelMovementListItemDto(
    Guid Id,
    Guid EmploymentId,
    Guid EmployeeId,
    string PersonnelNumber,
    string GivenName,
    string FamilyName,
    PersonnelMovementType Type,
    DateOnly EffectiveDate,
    PersonnelMovementLifecycle Lifecycle,
    string Reason,
    string? Note,
    AssignmentSummaryDto? PreviousAssignment,
    AssignmentSummaryDto? NewAssignment,
    ReportingLineSummaryDto? PreviousReportingLine,
    ReportingLineSummaryDto? NewReportingLine,
    string CreatedByUserId,
    DateTimeOffset CreatedAtUtc);

public sealed record PersonnelMovementDetailDto(
    Guid Id,
    Guid EmploymentId,
    Guid EmployeeId,
    string PersonnelNumber,
    string GivenName,
    string FamilyName,
    PersonnelMovementType Type,
    DateOnly EffectiveDate,
    PersonnelMovementLifecycle Lifecycle,
    string Reason,
    string? Note,
    AssignmentSummaryDto? PreviousAssignment,
    AssignmentSummaryDto? NewAssignment,
    ReportingLineSummaryDto? PreviousReportingLine,
    ReportingLineSummaryDto? NewReportingLine,
    string CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    string? CancelledByUserId,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason);

internal static class PersonnelMovementComposer
{
    public static async Task<PersonnelMovementDetailDto> ComposeAsync(
        IWorkforceStore store,
        PersonnelMovement movement,
        Property? calendarProperty,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var item = await ComposeListItemAsync(store, movement, calendarProperty, utcNow, cancellationToken);
        return new PersonnelMovementDetailDto(
            item.Id,
            item.EmploymentId,
            item.EmployeeId,
            item.PersonnelNumber,
            item.GivenName,
            item.FamilyName,
            item.Type,
            item.EffectiveDate,
            item.Lifecycle,
            item.Reason,
            item.Note,
            item.PreviousAssignment,
            item.NewAssignment,
            item.PreviousReportingLine,
            item.NewReportingLine,
            item.CreatedByUserId,
            item.CreatedAtUtc,
            movement.CancelledByUserId,
            movement.CancelledAtUtc,
            movement.CancellationReason);
    }

    public static async Task<PersonnelMovementListItemDto> ComposeListItemAsync(
        IWorkforceStore store,
        PersonnelMovement movement,
        Property? calendarProperty,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var employment = await store.GetEmploymentAsync(movement.EmploymentId, cancellationToken)
            ?? throw new InvalidOperationException("Employment missing for personnel movement.");
        var employee = await store.GetEmployeeAsync(employment.EmployeeId, cancellationToken)
            ?? throw new InvalidOperationException("Employee missing for personnel movement.");

        var previousAssignment = await AssignmentSummaryAsync(store, movement.PreviousAssignmentId, cancellationToken);
        var newAssignment = await AssignmentSummaryAsync(store, movement.NewAssignmentId, cancellationToken);
        var previousLine = await ReportingSummaryAsync(store, movement.PreviousReportingLineId, cancellationToken);
        var newLine = await ReportingSummaryAsync(store, movement.NewReportingLineId, cancellationToken);

        var zoneProperty = calendarProperty
            ?? await InferCalendarPropertyAsync(store, previousAssignment, newAssignment, cancellationToken);
        var today = zoneProperty is null
            ? DateOnly.FromDateTime(utcNow.UtcDateTime)
            : PropertyLocalCalendar.Today(utcNow, zoneProperty.TimeZoneId);

        return new PersonnelMovementListItemDto(
            movement.Id,
            movement.EmploymentId,
            employee.Id,
            employee.PersonnelNumber,
            employee.GivenName,
            employee.FamilyName,
            movement.MovementType,
            movement.EffectiveDate,
            movement.Lifecycle(today),
            movement.Reason,
            movement.Note,
            previousAssignment,
            newAssignment,
            previousLine,
            newLine,
            movement.CreatedByUserId,
            movement.CreatedAtUtc);
    }

    public static IEnumerable<Guid> PropertyIdsOf(PersonnelMovementListItemDto item)
    {
        if (item.PreviousAssignment is not null)
        {
            yield return item.PreviousAssignment.PropertyId;
        }

        if (item.NewAssignment is not null)
        {
            yield return item.NewAssignment.PropertyId;
        }
    }

    private static async Task<Property?> InferCalendarPropertyAsync(
        IWorkforceStore store,
        AssignmentSummaryDto? previous,
        AssignmentSummaryDto? next,
        CancellationToken cancellationToken)
    {
        var propertyId = next?.PropertyId ?? previous?.PropertyId;
        if (propertyId is null)
        {
            return null;
        }

        return await store.GetPropertyAsync(propertyId.Value, cancellationToken);
    }

    private static async Task<AssignmentSummaryDto?> AssignmentSummaryAsync(
        IWorkforceStore store,
        Guid? assignmentId,
        CancellationToken cancellationToken)
    {
        if (assignmentId is null)
        {
            return null;
        }

        var assignment = await store.GetAssignmentAsync(assignmentId.Value, cancellationToken);
        if (assignment is null)
        {
            return null;
        }

        var department = await store.GetDepartmentAsync(assignment.DepartmentId, cancellationToken);
        var position = await store.GetPositionAsync(assignment.PositionId, cancellationToken);
        if (department is null || position is null)
        {
            return null;
        }

        var property = await store.GetPropertyAsync(department.PropertyId, cancellationToken);
        if (property is null)
        {
            return null;
        }

        return new AssignmentSummaryDto(
            assignment.Id,
            department.Id,
            department.Name,
            position.Id,
            position.Name,
            property.Id,
            property.Name,
            assignment.StartDate,
            assignment.EndDate);
    }

    private static async Task<ReportingLineSummaryDto?> ReportingSummaryAsync(
        IWorkforceStore store,
        Guid? lineId,
        CancellationToken cancellationToken)
    {
        if (lineId is null)
        {
            return null;
        }

        var line = await store.GetReportingLineAsync(lineId.Value, cancellationToken);
        if (line is null)
        {
            return null;
        }

        var managerEmployment = await store.GetEmploymentAsync(line.ManagerEmploymentId, cancellationToken);
        if (managerEmployment is null)
        {
            return null;
        }

        var manager = await store.GetEmployeeAsync(managerEmployment.EmployeeId, cancellationToken);
        if (manager is null)
        {
            return null;
        }

        return new ReportingLineSummaryDto(
            line.Id,
            line.ManagerEmploymentId,
            manager.Id,
            manager.GivenName,
            manager.FamilyName,
            line.EffectiveFrom,
            line.EffectiveTo);
    }
}
