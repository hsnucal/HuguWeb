namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// HR-05B overlap: active Pending/Approved requests and Recorded LeaveRecords block
/// overlapping ranges on the same Employment. Rejected/Cancelled requests and Cancelled
/// records are ignored. Inclusive dates; half-days on the same date still overlap.
/// </summary>
public static class LeaveRequestOverlap
{
    public static bool OverlapsAnyActiveRequest(
        IEnumerable<LeaveRequest> existing,
        DateOnly startDate,
        DateOnly endDate,
        Guid? ignoreRequestId = null)
    {
        foreach (var request in existing)
        {
            if (request.Status is not (LeaveRequestStatus.Pending or LeaveRequestStatus.Approved))
            {
                continue;
            }

            if (ignoreRequestId is { } ignore && request.Id == ignore)
            {
                continue;
            }

            if (LeaveOverlap.RangesOverlap(startDate, endDate, request.StartDate, request.EndDate))
            {
                return true;
            }
        }

        return false;
    }

    public static bool BlocksCreateOrApprove(
        IEnumerable<LeaveRequest> existingRequests,
        IEnumerable<LeaveRecord> existingRecords,
        DateOnly startDate,
        DateOnly endDate,
        Guid? ignoreRequestId = null,
        Guid? ignoreRecordId = null) =>
        OverlapsAnyActiveRequest(existingRequests, startDate, endDate, ignoreRequestId)
        || LeaveOverlap.OverlapsAnyRecorded(existingRecords, startDate, endDate, ignoreRecordId);
}
