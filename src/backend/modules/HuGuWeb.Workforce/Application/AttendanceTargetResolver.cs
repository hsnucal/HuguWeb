using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// Resolves employment + dated assignment for attendance mutations/reads.
/// Uses the assignment covering LocalDate — never the current/first assignment.
/// Missing historical assignment is rejected (no silent fallback).
/// </summary>
internal static class AttendanceTargetResolver
{
    public static async Task<WorkforceResult<AttendanceTarget>> ResolveAsync(
        IWorkforceStore store,
        IWorkplaceContext workplaceContext,
        Guid employmentId,
        DateOnly localDate,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        CancellationToken cancellationToken)
    {
        if (!workplaceContext.HasOrganization)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var employment = await store.GetEmploymentAsync(employmentId, cancellationToken);
        if (employment is null)
        {
            return WorkforceError.AttendanceEmploymentNotFound();
        }

        var employee = await store.GetEmployeeAsync(employment.EmployeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplaceContext.OrganizationId)
        {
            return WorkforceError.AttendanceEmploymentNotFound();
        }

        if (!ScheduleEntry.IsWithinEmploymentPeriod(employment, localDate))
        {
            return WorkforceError.AttendanceOutsideEmployment();
        }

        var resolvedWorkplace = await ScheduleWorkplaceResolver.ResolveAsync(
            store,
            employment,
            localDate,
            cancellationToken);
        if (!resolvedWorkplace.IsSuccess)
        {
            if (resolvedWorkplace.Error!.Code == ScheduleValidation.Codes.ScheduleAssignmentNotFound)
            {
                return WorkforceError.AttendanceAssignmentNotFound();
            }

            if (resolvedWorkplace.Error.Code == ScheduleValidation.Codes.ScheduleEmploymentNotCoveringDate)
            {
                return WorkforceError.AttendanceOutsideEmployment();
            }

            return resolvedWorkplace.Error;
        }

        var context = resolvedWorkplace.Value!;
        if (context.Property.Id != workplace.Value!.Property.Id
            || (scopedPropertyId is { } scoped && scoped != context.Property.Id))
        {
            return WorkforceError.AttendancePropertyAccessDenied();
        }

        if (!ScheduleAccess.AllowsWorkplace(
                scopedPropertyId ?? workplace.Value.Property.Id,
                allowedDepartmentIds,
                context.Property.Id,
                context.Department.Id))
        {
            return WorkforceError.AttendanceDepartmentScopeDenied();
        }

        return new AttendanceTarget(
            workplace.Value.Organization.Id,
            employee,
            employment,
            context.Assignment,
            context.Department,
            context.Property);
    }
}

internal sealed record AttendanceTarget(
    Guid OrganizationId,
    Employee Employee,
    Employment Employment,
    Assignment Assignment,
    Department Department,
    Property Property);
