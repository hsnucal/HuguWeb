namespace HuGuWeb.TechnicalService.Domain;

public sealed class MaintenanceIssue
{
    public const int DescriptionMaxLength = 2000;
    public const int NoteMaxLength = 2000;
    public const int OriginNoteMaxLength = 500;

    private MaintenanceIssue()
    {
        Description = string.Empty;
    }

    private MaintenanceIssue(
        Guid id,
        Guid propertyId,
        Guid roomId,
        Guid categoryId,
        string description,
        MaintenancePriority priority,
        MaintenanceIssueStatus status,
        Guid? assignedEmployeeId,
        Guid? reportedByEmployeeId,
        string? originNote,
        bool blocksRoomUse,
        OutageClassification? outageClassification,
        string? resolutionNote,
        string? unableToResolveNote,
        PreparationImpact? preparationImpact,
        DateTimeOffset createdAt,
        DateTimeOffset? startedAt,
        DateTimeOffset? resolvedAt,
        int version)
    {
        Id = id;
        PropertyId = propertyId;
        RoomId = roomId;
        CategoryId = categoryId;
        Description = description;
        Priority = priority;
        Status = status;
        AssignedEmployeeId = assignedEmployeeId;
        ReportedByEmployeeId = reportedByEmployeeId;
        OriginNote = originNote;
        BlocksRoomUse = blocksRoomUse;
        OutageClassification = outageClassification;
        ResolutionNote = resolutionNote;
        UnableToResolveNote = unableToResolveNote;
        PreparationImpact = preparationImpact;
        CreatedAt = createdAt;
        StartedAt = startedAt;
        ResolvedAt = resolvedAt;
        Version = version;
    }

    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Description { get; private set; }
    public MaintenancePriority Priority { get; private set; }
    public MaintenanceIssueStatus Status { get; private set; }
    public Guid? AssignedEmployeeId { get; private set; }
    public Guid? ReportedByEmployeeId { get; private set; }
    public string? OriginNote { get; private set; }
    public bool BlocksRoomUse { get; private set; }
    public OutageClassification? OutageClassification { get; private set; }
    public string? ResolutionNote { get; private set; }
    public string? UnableToResolveNote { get; private set; }
    public PreparationImpact? PreparationImpact { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public int Version { get; private set; }

    public bool IsOperationallyOpen =>
        Status is MaintenanceIssueStatus.Open
            or MaintenanceIssueStatus.InProgress
            or MaintenanceIssueStatus.UnableToResolve;

    public static bool TryCreate(
        Guid id,
        Guid propertyId,
        Guid roomId,
        Guid categoryId,
        string? description,
        MaintenancePriority priority,
        Guid? assignedEmployeeId,
        Guid? reportedByEmployeeId,
        string? originNote,
        bool blocksRoomUse,
        OutageClassification? outageClassification,
        DateTimeOffset createdAt,
        out MaintenanceIssue? issue,
        out string? error)
    {
        issue = null;
        if (id == Guid.Empty || propertyId == Guid.Empty || roomId == Guid.Empty || categoryId == Guid.Empty)
        {
            error = "Issue identity is invalid.";
            return false;
        }

        if (!Enum.IsDefined(priority))
        {
            error = "Priority must be Normal, High, or Urgent.";
            return false;
        }

        if (!TryNormalizeDescription(description, out var normalizedDescription, out error))
        {
            return false;
        }

        if (!TryNormalizeOptionalNote(originNote, OriginNoteMaxLength, out var normalizedOrigin, out error))
        {
            return false;
        }

        if (!TryNormalizeBlocking(blocksRoomUse, outageClassification, out error))
        {
            return false;
        }

        if (assignedEmployeeId == Guid.Empty)
        {
            assignedEmployeeId = null;
        }

        if (reportedByEmployeeId == Guid.Empty)
        {
            reportedByEmployeeId = null;
        }

        issue = new MaintenanceIssue(
            id,
            propertyId,
            roomId,
            categoryId,
            normalizedDescription,
            priority,
            MaintenanceIssueStatus.Open,
            assignedEmployeeId,
            reportedByEmployeeId,
            normalizedOrigin,
            blocksRoomUse,
            outageClassification,
            resolutionNote: null,
            unableToResolveNote: null,
            preparationImpact: null,
            createdAt,
            startedAt: null,
            resolvedAt: null,
            version: 1);
        return true;
    }

    public bool TryAssign(Guid employeeId, out string? error)
    {
        if (!CanMutate(out error))
        {
            return false;
        }

        if (employeeId == Guid.Empty)
        {
            error = "An assigned employee is required.";
            return false;
        }

        AssignedEmployeeId = employeeId;
        Version++;
        error = null;
        return true;
    }

    public bool TryChangePriority(MaintenancePriority priority, out string? error)
    {
        if (!CanMutate(out error))
        {
            return false;
        }

        if (!Enum.IsDefined(priority))
        {
            error = "Priority must be Normal, High, or Urgent.";
            return false;
        }

        Priority = priority;
        Version++;
        error = null;
        return true;
    }

    public bool TryChangeBlocking(bool blocksRoomUse, OutageClassification? outageClassification, out string? error)
    {
        if (!CanMutate(out error))
        {
            return false;
        }

        if (!TryNormalizeBlocking(blocksRoomUse, outageClassification, out error))
        {
            return false;
        }

        BlocksRoomUse = blocksRoomUse;
        OutageClassification = outageClassification;
        Version++;
        return true;
    }

    public bool TryStart(DateTimeOffset occurredAt, out string? error)
    {
        if (Status != MaintenanceIssueStatus.Open)
        {
            error = "Work can only start when the issue is Open.";
            return false;
        }

        if (AssignedEmployeeId is null)
        {
            error = "An assigned employee is required before work can start.";
            return false;
        }

        Status = MaintenanceIssueStatus.InProgress;
        StartedAt ??= occurredAt;
        Version++;
        error = null;
        return true;
    }

    public bool TryMarkUnableToResolve(string? note, out string? error)
    {
        if (Status != MaintenanceIssueStatus.InProgress)
        {
            error = "Unable to resolve can only be recorded while work is In Progress.";
            return false;
        }

        if (!TryNormalizeRequiredNote(note, out var normalized, out error))
        {
            return false;
        }

        Status = MaintenanceIssueStatus.UnableToResolve;
        UnableToResolveNote = normalized;
        Version++;
        return true;
    }

    public bool TryResume(out string? error)
    {
        if (Status != MaintenanceIssueStatus.UnableToResolve)
        {
            error = "Work can only resume from Unable To Resolve.";
            return false;
        }

        if (AssignedEmployeeId is null)
        {
            error = "An assigned employee is required before work can resume.";
            return false;
        }

        Status = MaintenanceIssueStatus.InProgress;
        Version++;
        error = null;
        return true;
    }

    public bool TryResolve(string? note, PreparationImpact preparationImpact, DateTimeOffset occurredAt, out string? error)
    {
        if (Status != MaintenanceIssueStatus.InProgress)
        {
            error = "An issue can only be resolved while work is In Progress.";
            return false;
        }

        if (!Enum.IsDefined(preparationImpact))
        {
            error = "Preparation impact must be None or RequiresPreparation.";
            return false;
        }

        if (!TryNormalizeRequiredNote(note, out var normalized, out error))
        {
            return false;
        }

        Status = MaintenanceIssueStatus.Resolved;
        ResolutionNote = normalized;
        PreparationImpact = preparationImpact;
        ResolvedAt = occurredAt;
        Version++;
        return true;
    }

    private bool CanMutate(out string? error)
    {
        if (Status == MaintenanceIssueStatus.Resolved)
        {
            error = "A resolved issue cannot be changed.";
            return false;
        }

        error = null;
        return true;
    }

    public static bool TryNormalizeDescription(string? description, out string normalized, out string? error) =>
        TryNormalizeRequiredText(description, DescriptionMaxLength, "Issue description", out normalized, out error);

    public static bool TryNormalizeRequiredNote(string? note, out string normalized, out string? error) =>
        TryNormalizeRequiredText(note, NoteMaxLength, "Note", out normalized, out error);

    public static bool TryNormalizeOptionalNote(string? note, int maxLength, out string? normalized, out string? error)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(note))
        {
            error = null;
            return true;
        }

        var trimmed = note.Trim();
        if (trimmed.Length > maxLength)
        {
            error = $"Note must be {maxLength} characters or fewer.";
            return false;
        }

        normalized = trimmed;
        error = null;
        return true;
    }

    public static bool TryNormalizeBlocking(
        bool blocksRoomUse,
        OutageClassification? outageClassification,
        out string? error)
    {
        if (blocksRoomUse)
        {
            if (outageClassification is null || !Enum.IsDefined(outageClassification.Value))
            {
                error = "A blocking issue requires Out of Order or Out of Service.";
                return false;
            }

            error = null;
            return true;
        }

        if (outageClassification is not null)
        {
            error = "Outage classification is only allowed when the issue blocks room use.";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryNormalizeRequiredText(
        string? value,
        int maxLength,
        string fieldLabel,
        out string normalized,
        out string? error)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            error = $"{fieldLabel} is required.";
            return false;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            error = $"{fieldLabel} must be {maxLength} characters or fewer.";
            return false;
        }

        normalized = trimmed;
        error = null;
        return true;
    }
}
