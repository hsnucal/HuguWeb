using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class ClearScheduleEntryUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    /// <summary>
    /// Clears authoritative schedule presence → Unscheduled. Already-Unscheduled is a stable no-op success.
    /// History is retained.
    /// </summary>
    public Task<WorkforceResult<ScheduleStateDto>> ExecuteAsync(
        ClearScheduleEntryCommand command,
        CancellationToken cancellationToken) =>
        ExecuteCoreAsync(command, saveChanges: true, cancellationToken);

    /// <summary>
    /// Same rules as <see cref="ExecuteAsync"/>. When <paramref name="saveChanges"/> is false,
    /// callers (bulk/copy) own the transaction boundary.
    /// </summary>
    internal async Task<WorkforceResult<ScheduleStateDto>> ExecuteCoreAsync(
        ClearScheduleEntryCommand command,
        bool saveChanges,
        CancellationToken cancellationToken)
    {
        if (!workplaceContext.HasOrganization)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        var employee = await store.GetEmployeeAsync(command.EmployeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplaceContext.OrganizationId)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var employments = await store.ListEmploymentsAsync(command.EmployeeId, cancellationToken);
        var employmentResult = ScheduleEmploymentResolver.ResolveCovering(employments, command.ScheduleDate);
        if (!employmentResult.IsSuccess)
        {
            return employmentResult.Error!;
        }

        var workplace = await ScheduleWorkplaceResolver.ResolveAsync(
            store,
            employmentResult.Value!,
            command.ScheduleDate,
            cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        if (!ScheduleAccess.AllowsWorkplace(
                command.ScopedPropertyId,
                command.AllowedDepartmentIds,
                workplace.Value!.Property.Id,
                workplace.Value.Department.Id))
        {
            return WorkforceError.SchedulePropertyAccessDenied();
        }

        var existing = await store.GetScheduleEntryAsync(
            workplace.Value!.Employment.Id,
            command.ScheduleDate,
            cancellationToken);

        if (existing is null)
        {
            // Idempotent clear: already Unscheduled.
            return ScheduleStateDto.Unscheduled(command.ScheduleDate, workplace.Value);
        }

        store.AddScheduleEntryChange(
            ScheduleEntryChange.FromMutation(
                Guid.CreateVersion7(),
                previous: existing,
                next: null,
                workplace.Value.Employment.Id,
                command.ScheduleDate,
                command.ActorUserId,
                clock.UtcNow));
        store.RemoveScheduleEntry(existing);
        if (saveChanges)
        {
            await store.SaveChangesAsync(cancellationToken);
        }

        return ScheduleStateDto.Unscheduled(command.ScheduleDate, workplace.Value);
    }
}

public sealed record ClearScheduleEntryCommand(
    Guid EmployeeId,
    DateOnly ScheduleDate,
    string ActorUserId,
    Guid? ScopedPropertyId,
    IReadOnlySet<Guid>? AllowedDepartmentIds = null);
