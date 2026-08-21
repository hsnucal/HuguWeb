namespace HuGuWeb.Workforce.Domain;

public sealed class Assignment
{
    private Assignment()
    {
    }

    private Assignment(
        Guid id,
        Guid employmentId,
        Guid departmentId,
        Guid positionId,
        DateOnly startDate,
        DateOnly? endDate,
        AssignmentKind kind)
    {
        Id = id;
        EmploymentId = employmentId;
        DepartmentId = departmentId;
        PositionId = positionId;
        StartDate = startDate;
        EndDate = endDate;
        Kind = kind;
    }

    public Guid Id { get; private set; }
    public Guid EmploymentId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid PositionId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public AssignmentKind Kind { get; private set; }

    public DatePeriod Period => new(StartDate, EndDate);

    public static Assignment StartPrimary(
        Guid id,
        Guid employmentId,
        Guid departmentId,
        Guid positionId,
        DateOnly startDate) =>
        new(id, employmentId, departmentId, positionId, startDate, endDate: null, AssignmentKind.Primary);

    public bool Covers(DateOnly date) => Period.Contains(date);

    public bool TryCloseOn(DateOnly endDate, out string? error)
    {
        if (endDate < StartDate)
        {
            error = "Assignment end date must be on or after the start date.";
            return false;
        }

        EndDate = endDate;
        error = null;
        return true;
    }
}
