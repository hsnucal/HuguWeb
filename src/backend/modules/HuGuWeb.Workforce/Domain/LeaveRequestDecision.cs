namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Append-only audit row for leave-request workflow actions. Never overwrite historical rows.
/// </summary>
public sealed class LeaveRequestDecision
{
    public const int NoteMaxLength = 500;
    public const int UserIdMaxLength = 450;

    private LeaveRequestDecision()
    {
        ActorUserId = string.Empty;
    }

    private LeaveRequestDecision(
        Guid id,
        Guid leaveRequestId,
        LeaveRequestApprovalStage stage,
        LeaveRequestDecisionKind decision,
        string actorUserId,
        DateTimeOffset decisionAtUtc,
        string? note)
    {
        Id = id;
        LeaveRequestId = leaveRequestId;
        Stage = stage;
        Decision = decision;
        ActorUserId = actorUserId;
        DecisionAtUtc = decisionAtUtc;
        Note = note;
    }

    public Guid Id { get; private set; }
    public Guid LeaveRequestId { get; private set; }
    public LeaveRequestApprovalStage Stage { get; private set; }
    public LeaveRequestDecisionKind Decision { get; private set; }
    public string ActorUserId { get; private set; }
    public DateTimeOffset DecisionAtUtc { get; private set; }
    public string? Note { get; private set; }

    public static LeaveRequestDecision Create(
        Guid id,
        Guid leaveRequestId,
        LeaveRequestApprovalStage stage,
        LeaveRequestDecisionKind decision,
        string actorUserId,
        DateTimeOffset decisionAtUtc,
        string? note)
    {
        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmed is { Length: > NoteMaxLength })
        {
            trimmed = trimmed[..NoteMaxLength];
        }

        return new LeaveRequestDecision(
            id,
            leaveRequestId,
            stage,
            decision,
            actorUserId,
            decisionAtUtc,
            trimmed);
    }
}
