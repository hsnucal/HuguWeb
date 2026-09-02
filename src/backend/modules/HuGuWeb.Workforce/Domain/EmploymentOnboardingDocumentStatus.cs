namespace HuGuWeb.Workforce.Domain;

public sealed class EmploymentOnboardingDocumentStatus
{
    public const int UserIdMaxLength = 450;

    private EmploymentOnboardingDocumentStatus()
    {
    }

    private EmploymentOnboardingDocumentStatus(
        Guid id,
        Guid employmentId,
        Guid requirementId,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        EmploymentId = employmentId;
        RequirementId = requirementId;
        IsCompleted = false;
        UpdatedAtUtc = updatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid EmploymentId { get; private set; }
    public Guid RequirementId { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public string? CompletedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static EmploymentOnboardingDocumentStatus Create(
        Guid id,
        Guid employmentId,
        Guid requirementId,
        DateTimeOffset utcNow) =>
        new(id, employmentId, requirementId, utcNow);

    public void MarkCompleted(string actorUserId, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorUserId);
        IsCompleted = true;
        CompletedAtUtc = utcNow;
        CompletedByUserId = actorUserId.Trim();
        UpdatedAtUtc = utcNow;
    }

    public void MarkIncomplete(DateTimeOffset utcNow)
    {
        IsCompleted = false;
        CompletedAtUtc = null;
        CompletedByUserId = null;
        UpdatedAtUtc = utcNow;
    }
}
