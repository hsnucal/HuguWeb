namespace HuGuWeb.RoomOperations.Domain;

public sealed class Room
{
    public const int NumberMaxLength = 32;

    private Room()
    {
        Number = string.Empty;
    }

    private Room(
        Guid id,
        Guid propertyId,
        string number,
        bool isActive,
        RoomReadiness readiness,
        Guid readinessCycleId,
        int readinessVersion)
    {
        Id = id;
        PropertyId = propertyId;
        Number = number;
        IsActive = isActive;
        CurrentReadiness = readiness;
        ReadinessCycleId = readinessCycleId;
        ReadinessVersion = readinessVersion;
    }

    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Number { get; private set; }
    public bool IsActive { get; private set; }
    public RoomReadiness CurrentReadiness { get; private set; }
    public Guid ReadinessCycleId { get; private set; }
    public int ReadinessVersion { get; private set; }

    public static bool TryCreate(
        Guid id,
        Guid propertyId,
        string? number,
        Guid readinessCycleId,
        out Room? room,
        out string? error)
    {
        room = null;
        if (id == Guid.Empty || propertyId == Guid.Empty || readinessCycleId == Guid.Empty)
        {
            error = "Room identity is invalid.";
            return false;
        }

        if (!TryNormalizeNumber(number, out var normalized, out error))
        {
            return false;
        }

        room = new Room(
            id,
            propertyId,
            normalized,
            isActive: true,
            RoomReadiness.Dirty,
            readinessCycleId,
            readinessVersion: 1);
        return true;
    }

    public bool TryMarkDirtyForNewCycle(Guid newCycleId, out string? error)
    {
        if (!IsActive)
        {
            error = "An inactive room cannot receive new preparation work.";
            return false;
        }

        if (newCycleId == Guid.Empty || newCycleId == ReadinessCycleId)
        {
            error = "A new readiness cycle is required.";
            return false;
        }

        CurrentReadiness = RoomReadiness.Dirty;
        ReadinessCycleId = newCycleId;
        ReadinessVersion++;
        error = null;
        return true;
    }

    public bool TryMarkClean(Guid expectedCycleId, out string? error)
    {
        if (CurrentReadiness != RoomReadiness.Dirty)
        {
            error = "Cleaning can only be completed when the room is Dirty.";
            return false;
        }

        if (expectedCycleId != ReadinessCycleId)
        {
            error = "This work item is not current for the room.";
            return false;
        }

        CurrentReadiness = RoomReadiness.Clean;
        ReadinessVersion++;
        error = null;
        return true;
    }

    public bool TryMarkInspected(out string? error)
    {
        if (CurrentReadiness != RoomReadiness.Clean)
        {
            error = "Inspection can only be accepted when the room is Clean.";
            return false;
        }

        CurrentReadiness = RoomReadiness.Inspected;
        ReadinessVersion++;
        error = null;
        return true;
    }

    public bool TryMarkReady(out string? error)
    {
        if (CurrentReadiness != RoomReadiness.Inspected)
        {
            error = "A room cannot become Ready without being Inspected.";
            return false;
        }

        CurrentReadiness = RoomReadiness.Ready;
        ReadinessVersion++;
        error = null;
        return true;
    }

    public bool CanReceiveNeedsCleaningWork(out string? error)
    {
        if (!IsActive)
        {
            error = "An inactive room cannot receive new preparation work.";
            return false;
        }

        error = null;
        return true;
    }

    public static bool TryNormalizeNumber(string? number, out string normalized, out string? error)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(number))
        {
            error = "Room number is required.";
            return false;
        }

        var trimmed = number.Trim();
        if (trimmed.Length > NumberMaxLength)
        {
            error = $"Room number must be {NumberMaxLength} characters or fewer.";
            return false;
        }

        normalized = trimmed;
        error = null;
        return true;
    }
}
