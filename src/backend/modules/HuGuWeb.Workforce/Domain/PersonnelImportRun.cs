namespace HuGuWeb.Workforce.Domain;

public sealed class PersonnelImportRun
{
    public const int FileNameMaxLength = 260;
    public const int ActorUserIdMaxLength = 450;

    private PersonnelImportRun()
    {
        FileName = string.Empty;
        ActorUserId = string.Empty;
    }

    private PersonnelImportRun(
        Guid id,
        Guid organizationId,
        Guid propertyId,
        string fileName,
        int rowCount,
        int createdCount,
        int updatedCount,
        int failedCount,
        string actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        PropertyId = propertyId;
        FileName = fileName;
        RowCount = rowCount;
        CreatedCount = createdCount;
        UpdatedCount = updatedCount;
        FailedCount = failedCount;
        ActorUserId = actorUserId;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string FileName { get; private set; }
    public int RowCount { get; private set; }
    public int CreatedCount { get; private set; }
    public int UpdatedCount { get; private set; }
    public int FailedCount { get; private set; }
    public string ActorUserId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static PersonnelImportRun Create(
        Guid id,
        Guid organizationId,
        Guid propertyId,
        string fileName,
        int rowCount,
        int createdCount,
        int updatedCount,
        int failedCount,
        string actorUserId,
        DateTimeOffset occurredAtUtc)
    {
        var trimmedName = fileName.Trim();
        if (trimmedName.Length > FileNameMaxLength)
        {
            trimmedName = trimmedName[..FileNameMaxLength];
        }

        return new PersonnelImportRun(
            id,
            organizationId,
            propertyId,
            trimmedName,
            rowCount,
            createdCount,
            updatedCount,
            failedCount,
            actorUserId,
            occurredAtUtc);
    }
}
