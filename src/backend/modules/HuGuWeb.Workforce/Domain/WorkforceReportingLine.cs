namespace HuGuWeb.Workforce.Domain;

public sealed class WorkforceReportingLine
{
    private WorkforceReportingLine()
    {
    }

    private WorkforceReportingLine(
        Guid id,
        Guid organizationId,
        Guid subordinateEmploymentId,
        Guid managerEmploymentId,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo)
    {
        Id = id;
        OrganizationId = organizationId;
        SubordinateEmploymentId = subordinateEmploymentId;
        ManagerEmploymentId = managerEmploymentId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid SubordinateEmploymentId { get; private set; }
    public Guid ManagerEmploymentId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    public DatePeriod Period => new(EffectiveFrom, EffectiveTo);

    public static WorkforceReportingLine Start(
        Guid id,
        Guid organizationId,
        Guid subordinateEmploymentId,
        Guid managerEmploymentId,
        DateOnly effectiveFrom) =>
        new(id, organizationId, subordinateEmploymentId, managerEmploymentId, effectiveFrom, effectiveTo: null);

    public bool Covers(DateOnly date) => Period.Contains(date);

    public bool TryCloseOn(DateOnly endDate, out string? error)
    {
        if (endDate < EffectiveFrom)
        {
            error = "Reporting line end date must be on or after the start date.";
            return false;
        }

        EffectiveTo = endDate;
        error = null;
        return true;
    }

    public void Reopen() => EffectiveTo = null;

    internal void RestoreEffectiveTo(DateOnly? effectiveTo) => EffectiveTo = effectiveTo;
}
