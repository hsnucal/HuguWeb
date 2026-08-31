namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Resolves and validates the Primary Assignment for a leave request date range.
/// Client never supplies AssignmentId; StartDate drives resolution; the same Assignment
/// must cover the entire inclusive range.
/// </summary>
public static class LeaveRequestAssignment
{
    public static bool TryResolveForRange(
        IReadOnlyList<Assignment> assignments,
        DateOnly startDate,
        DateOnly endDate,
        out Assignment? assignment,
        out string? errorCode)
    {
        assignment = null;
        errorCode = null;

        var coveringStart = PrimaryAssignments.Covering(assignments, startDate);
        if (coveringStart is null)
        {
            errorCode = LeaveValidation.Codes.LeaveRequestAssignmentNotFound;
            return false;
        }

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var covering = PrimaryAssignments.Covering(assignments, date);
            if (covering is null || covering.Id != coveringStart.Id)
            {
                errorCode = LeaveValidation.Codes.LeaveRequestCrossAssignmentRange;
                assignment = null;
                return false;
            }
        }

        assignment = coveringStart;
        return true;
    }
}
