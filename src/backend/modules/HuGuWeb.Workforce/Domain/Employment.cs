namespace HuGuWeb.Workforce.Domain;

public sealed class Employment
{
    private Employment()
    {
    }

    private Employment(Guid id, Guid employeeId, DateOnly startDate, DateOnly? endDate, EmploymentStatus status)
    {
        Id = id;
        EmployeeId = employeeId;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
    }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public EmploymentStatus Status { get; private set; }

    public DatePeriod Period => new(StartDate, EndDate);

    public bool IsEnded => Status == EmploymentStatus.Ended;

    public static Employment Open(Guid id, Guid employeeId, DateOnly startDate, DateOnly today)
    {
        var status = startDate > today ? EmploymentStatus.Scheduled : EmploymentStatus.Active;
        return new Employment(id, employeeId, startDate, endDate: null, status);
    }

    public EmploymentStatus EffectiveStatus(DateOnly today)
    {
        if (Status == EmploymentStatus.Ended)
        {
            return EmploymentStatus.Ended;
        }

        return startDateIsFuture(today) ? EmploymentStatus.Scheduled : EmploymentStatus.Active;
    }

    public bool TryEnd(DateOnly endDate, out string? error)
    {
        if (IsEnded)
        {
            error = "Employment is already ended.";
            return false;
        }

        if (endDate < StartDate)
        {
            error = "Employment end date must be on or after the start date.";
            return false;
        }

        EndDate = endDate;
        Status = EmploymentStatus.Ended;
        error = null;
        return true;
    }

    public bool TryEnsureAssignmentFits(DatePeriod assignmentPeriod, out string? error)
    {
        if (!assignmentPeriod.IsValid)
        {
            error = "Assignment end date must be on or after the start date.";
            return false;
        }

        if (assignmentPeriod.Start < StartDate)
        {
            error = "A primary assignment must stay within the employment period.";
            return false;
        }

        if (EndDate is { } employmentEnd && assignmentPeriod.Start > employmentEnd)
        {
            error = "A primary assignment must stay within the employment period.";
            return false;
        }

        if (assignmentPeriod.End is { } assignmentEnd && EndDate is { } closed && assignmentEnd > closed)
        {
            error = "A primary assignment must stay within the employment period.";
            return false;
        }

        error = null;
        return true;
    }

    public void RefreshLifecycle(DateOnly today)
    {
        if (Status == EmploymentStatus.Ended)
        {
            return;
        }

        Status = startDateIsFuture(today) ? EmploymentStatus.Scheduled : EmploymentStatus.Active;
    }

    private bool startDateIsFuture(DateOnly today) => StartDate > today;
}
