namespace HuGuWeb.TechnicalService.Domain;

public enum RoomServiceabilityState
{
    Serviceable = 0,
    OutOfOrder = 1,
    OutOfService = 2
}

public static class RoomServiceability
{
    public static RoomServiceabilityState Derive(IEnumerable<MaintenanceIssue> issues)
    {
        var blocking = issues.Where(issue => issue.IsOperationallyOpen && issue.BlocksRoomUse).ToArray();
        if (blocking.Any(issue => issue.OutageClassification == OutageClassification.OutOfService))
        {
            return RoomServiceabilityState.OutOfService;
        }

        if (blocking.Any(issue => issue.OutageClassification == OutageClassification.OutOfOrder))
        {
            return RoomServiceabilityState.OutOfOrder;
        }

        return RoomServiceabilityState.Serviceable;
    }

    public static MaintenanceIssue? GoverningIssue(IEnumerable<MaintenanceIssue> issues)
    {
        var blocking = issues.Where(issue => issue.IsOperationallyOpen && issue.BlocksRoomUse).ToArray();
        return FirstByAge(blocking, OutageClassification.OutOfService)
            ?? FirstByAge(blocking, OutageClassification.OutOfOrder);
    }

    private static MaintenanceIssue? FirstByAge(
        IEnumerable<MaintenanceIssue> blocking,
        OutageClassification classification) =>
        blocking
            .Where(issue => issue.OutageClassification == classification)
            .OrderBy(issue => issue.CreatedAt)
            .ThenBy(issue => issue.Id)
            .FirstOrDefault();
}
