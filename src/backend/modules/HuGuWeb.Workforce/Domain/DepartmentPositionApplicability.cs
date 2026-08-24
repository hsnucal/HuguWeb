namespace HuGuWeb.Workforce.Domain;

public sealed class DepartmentPositionApplicability
{
    private DepartmentPositionApplicability()
    {
    }

    public DepartmentPositionApplicability(Guid departmentId, Guid positionId)
    {
        DepartmentId = departmentId;
        PositionId = positionId;
    }

    public Guid DepartmentId { get; private set; }
    public Guid PositionId { get; private set; }
}
