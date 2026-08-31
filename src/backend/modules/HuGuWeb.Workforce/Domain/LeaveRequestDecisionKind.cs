namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Append-only decision outcome on a leave request. Withdraw and approved-cancellation
/// both use <see cref="Cancelled"/>; distinguish via the stage recorded on the decision row
/// and the request's prior status.
/// </summary>
public enum LeaveRequestDecisionKind
{
    Approved = 0,
    Rejected = 1,
    Cancelled = 2
}
