namespace HuGuWeb.Workforce.Domain;

public enum PersonnelMovementType
{
    DepartmentChange = 0,
    PositionChange = 1,
    Promotion = 2,
    PropertyTransfer = 3,
    ManagerChange = 4,
    /// <summary>
    /// Legacy Personnel Card Transfer when both department and position change in the same property.
    /// Not accepted on POST /api/hr/movements.
    /// </summary>
    AssignmentChange = 5
}

public enum PersonnelMovementLifecycle
{
    Scheduled = 0,
    Effective = 1,
    Cancelled = 2
}
