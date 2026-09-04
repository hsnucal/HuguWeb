namespace HuGuWeb.Workforce.Domain;

public static class ManagerHierarchy
{
    public static int? RequiredManagerLevel(
        IEnumerable<Position> activePositions,
        int subordinateOrganizationalLevel)
    {
        ArgumentNullException.ThrowIfNull(activePositions);

        int? required = null;
        foreach (var position in activePositions)
        {
            if (!position.IsActive)
            {
                continue;
            }

            var level = position.OrganizationalLevel;
            if (level <= subordinateOrganizationalLevel)
            {
                continue;
            }

            if (required is null || level < required.Value)
            {
                required = level;
            }
        }

        return required;
    }

    public static bool IsEligibleDirectManager(
        Position candidatePosition,
        int requiredManagerLevel) =>
        candidatePosition.IsActive
        && candidatePosition.CanManageEmployees
        && candidatePosition.OrganizationalLevel == requiredManagerLevel;
}
