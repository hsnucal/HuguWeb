namespace HuGuWeb.RoomOperations.Domain;

public sealed class HousekeepingWorkItem
{
    private HousekeepingWorkItem()
    {
    }

    private HousekeepingWorkItem(
        Guid id,
        Guid roomId,
        Guid readinessCycleId,
        Guid assignedEmployeeId,
        TaskPriority priority,
        HousekeepingWorkState state,
        HousekeepingWorkOrigin origin,
        DateTimeOffset createdAt,
        DateTimeOffset? completedAt,
        Guid? completedByEmployeeId,
        Guid? sourceInspectionId)
    {
        Id = id;
        RoomId = roomId;
        ReadinessCycleId = readinessCycleId;
        AssignedEmployeeId = assignedEmployeeId;
        Priority = priority;
        State = state;
        Origin = origin;
        CreatedAt = createdAt;
        CompletedAt = completedAt;
        CompletedByEmployeeId = completedByEmployeeId;
        SourceInspectionId = sourceInspectionId;
    }

    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid ReadinessCycleId { get; private set; }
    public Guid AssignedEmployeeId { get; private set; }
    public TaskPriority Priority { get; private set; }
    public HousekeepingWorkState State { get; private set; }
    public HousekeepingWorkOrigin Origin { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? CompletedByEmployeeId { get; private set; }
    public Guid? SourceInspectionId { get; private set; }

    public bool IsOpen => State == HousekeepingWorkState.Open;

    public static HousekeepingWorkItem Open(
        Guid id,
        Guid roomId,
        Guid readinessCycleId,
        Guid assignedEmployeeId,
        TaskPriority priority,
        DateTimeOffset createdAt,
        HousekeepingWorkOrigin origin = HousekeepingWorkOrigin.NeedsCleaning,
        Guid? sourceInspectionId = null)
    {
        if (id == Guid.Empty || roomId == Guid.Empty || readinessCycleId == Guid.Empty || assignedEmployeeId == Guid.Empty)
        {
            throw new ArgumentException("Housekeeping work identity is invalid.");
        }

        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(nameof(priority));
        }

        return new HousekeepingWorkItem(
            id,
            roomId,
            readinessCycleId,
            assignedEmployeeId,
            priority,
            HousekeepingWorkState.Open,
            origin,
            createdAt,
            completedAt: null,
            completedByEmployeeId: null,
            sourceInspectionId);
    }

    public bool TryComplete(Guid completedByEmployeeId, DateTimeOffset completedAt, Guid expectedCycleId, out string? error)
    {
        if (!IsOpen)
        {
            error = "This work item is not current.";
            return false;
        }

        if (expectedCycleId != ReadinessCycleId)
        {
            error = "This work item is not current for the room.";
            return false;
        }

        if (completedByEmployeeId == Guid.Empty)
        {
            error = "A responsible employee is required.";
            return false;
        }

        State = HousekeepingWorkState.Completed;
        CompletedAt = completedAt;
        CompletedByEmployeeId = completedByEmployeeId;
        error = null;
        return true;
    }
}
