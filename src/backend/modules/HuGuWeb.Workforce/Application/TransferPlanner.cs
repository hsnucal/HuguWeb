using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public static class AssignmentDestination
{
    public static WorkforceResult<Position> Ensure(Department department, Position position, bool isApplicable)
    {
        if (!department.IsActive)
        {
            return WorkforceError.DepartmentInactive();
        }

        if (!position.IsActive)
        {
            return WorkforceError.PositionInactive();
        }

        if (!isApplicable)
        {
            return WorkforceError.PositionNotAvailableForDepartment();
        }

        return position;
    }
}

public static class TransferPlanner
{
    public static WorkforceResult<TransferPlan> Plan(
        Employment employment,
        IReadOnlyList<Assignment> primaryAssignments,
        Department department,
        Position position,
        bool isApplicable,
        DateOnly effectiveDate)
    {
        if (employment.IsEnded)
        {
            return WorkforceError.EmploymentEnded();
        }

        var destination = AssignmentDestination.Ensure(department, position, isApplicable);
        if (!destination.IsSuccess)
        {
            return destination.Error!;
        }

        var newPeriod = new DatePeriod(effectiveDate, null);
        if (!employment.TryEnsureAssignmentFits(newPeriod, out _))
        {
            return WorkforceError.AssignmentOutsideEmployment();
        }

        var overlapping = PrimaryAssignments.OrderedPrimaries(primaryAssignments)
            .Where(assignment => assignment.Period.Overlaps(newPeriod))
            .ToArray();

        if (overlapping.Length == 0)
        {
            return WorkforceError.InvalidTransferDate();
        }

        if (overlapping.Length > 1)
        {
            return WorkforceError.OverlappingPrimaryAssignment();
        }

        var current = overlapping[0];
        if (current.DepartmentId == department.Id && current.PositionId == position.Id)
        {
            return WorkforceError.SameAssignment();
        }

        var previousEnd = effectiveDate.AddDays(-1);
        if (previousEnd < current.StartDate)
        {
            return WorkforceError.InvalidTransferDate();
        }

        return new TransferPlan(current, previousEnd, department.Id, position.Id, effectiveDate);
    }

    public static WorkforceResult CloseForEmploymentEnd(
        Employment employment,
        IReadOnlyList<Assignment> primaryAssignments,
        DateOnly endDate)
    {
        foreach (var assignment in PrimaryAssignments.OrderedPrimaries(primaryAssignments))
        {
            if (assignment.StartDate > endDate)
            {
                return WorkforceError.AssignmentOutsideEmployment();
            }

            var alreadyClosed = assignment.EndDate is { } existing && existing <= endDate;
            if (alreadyClosed)
            {
                continue;
            }

            if (!assignment.TryCloseOn(endDate, out var error))
            {
                return error == "Assignment end date must be on or after the start date."
                    ? WorkforceError.InvalidAssignmentPeriod()
                    : WorkforceError.AssignmentOutsideEmployment();
            }

            if (!employment.TryEnsureAssignmentFits(assignment.Period, out _))
            {
                return WorkforceError.AssignmentOutsideEmployment();
            }
        }

        return WorkforceResult.Success();
    }
}

public sealed record TransferPlan(
    Assignment CurrentPrimary,
    DateOnly PreviousEndDate,
    Guid NewDepartmentId,
    Guid NewPositionId,
    DateOnly NewStartDate);
