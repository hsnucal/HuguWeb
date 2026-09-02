namespace HuGuWeb.Workforce.Domain;

public sealed class Employment
{
    public const int AllowedProbationPeriodMonths = 2;

    private Employment()
    {
    }

    private Employment(
        Guid id,
        Guid employeeId,
        DateOnly startDate,
        DateOnly? endDate,
        EmploymentStatus status,
        WorkType workType,
        EmploymentOnboardingStatus onboardingStatus)
    {
        Id = id;
        EmployeeId = employeeId;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        WorkType = workType;
        OnboardingStatus = onboardingStatus;
    }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public EmploymentStatus Status { get; private set; }
    public DateOnly? SeniorityStartDate { get; private set; }
    public EmploymentTerminationReason? TerminationReason { get; private set; }
    public EmploymentContractType? ContractType { get; private set; }
    public DateOnly? ContractEndDate { get; private set; }
    public decimal? PartTimeMonthlyHours { get; private set; }
    public IskurStatus? IskurStatus { get; private set; }
    public DateOnly? IncentiveStartDate { get; private set; }
    public DateOnly? IncentiveEndDate { get; private set; }
    public IskurWorkforceStatus? IskurWorkforceStatus { get; private set; }
    public DateOnly? WorkPermitStartDate { get; private set; }
    public DateOnly? WorkPermitEndDate { get; private set; }
    public WorkType WorkType { get; private set; }
    public int? ProbationPeriodMonths { get; private set; }
    public DateOnly? ProbationStartDate { get; private set; }
    public Guid? RecruitmentSourceId { get; private set; }
    public EmploymentOnboardingStatus OnboardingStatus { get; private set; }

    public DatePeriod Period => new(StartDate, EndDate);

    public bool IsEnded => Status == EmploymentStatus.Ended;

    public bool IsOnboardingMutable => OnboardingStatus == EmploymentOnboardingStatus.InProgress;

    public DateOnly EffectiveSeniorityDate => SeniorityStartDate ?? StartDate;

    public DateOnly? ProbationEndDate =>
        ProbationPeriodMonths == AllowedProbationPeriodMonths && ProbationStartDate is { } start
            ? start.AddMonths(AllowedProbationPeriodMonths)
            : null;

    public static Employment Open(
        Guid id,
        Guid employeeId,
        DateOnly startDate,
        DateOnly today,
        WorkType workType = WorkType.FullTime)
    {
        if (!Enum.IsDefined(workType))
        {
            workType = WorkType.FullTime;
        }

        var status = startDate > today ? EmploymentStatus.Scheduled : EmploymentStatus.Active;
        return new Employment(
            id,
            employeeId,
            startDate,
            endDate: null,
            status,
            workType,
            EmploymentOnboardingStatus.InProgress);
    }

    public bool TryCompleteOnboarding(out string? error)
    {
        if (OnboardingStatus == EmploymentOnboardingStatus.Completed)
        {
            error = "Onboarding is already completed.";
            return false;
        }

        OnboardingStatus = EmploymentOnboardingStatus.Completed;
        error = null;
        return true;
    }

    public EmploymentStatus EffectiveStatus(DateOnly today)
    {
        if (Status == EmploymentStatus.Ended)
        {
            return EmploymentStatus.Ended;
        }

        return startDateIsFuture(today) ? EmploymentStatus.Scheduled : EmploymentStatus.Active;
    }

    public bool TryEnd(DateOnly endDate, EmploymentTerminationReason reason, out string? error)
    {
        if (IsEnded)
        {
            error = "Employment is already ended.";
            return false;
        }

        if (!Enum.IsDefined(reason))
        {
            error = "Termination reason is invalid.";
            return false;
        }

        if (endDate < StartDate)
        {
            error = "Employment end date must be on or after the start date.";
            return false;
        }

        EndDate = endDate;
        Status = EmploymentStatus.Ended;
        TerminationReason = reason;
        error = null;
        return true;
    }

    public bool TryApplySeniorityStartDate(DateOnly? seniorityStartDate, out string? field, out string? code)
    {
        field = null;
        code = null;

        if (seniorityStartDate is { } seniority && seniority > StartDate)
        {
            field = HrValidation.Fields.SeniorityStartDate;
            code = HrValidation.Codes.SeniorityStartDateInvalid;
            return false;
        }

        SeniorityStartDate = seniorityStartDate;
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

    public bool TryApplyWorkforceTerms(EmploymentWorkforceTermsValues values, out string? field, out string? code)
    {
        field = null;
        code = null;

        var workType = values.WorkType ?? WorkType;
        if (!Enum.IsDefined(workType))
        {
            field = HrValidation.Fields.WorkType;
            code = HrValidation.Codes.WorkTypeInvalid;
            return false;
        }

        var probationMonths = values.ProbationPeriodMonths;
        if (probationMonths is not null
            && probationMonths != AllowedProbationPeriodMonths)
        {
            field = HrValidation.Fields.ProbationPeriodMonths;
            code = HrValidation.Codes.ProbationPeriodMonthsInvalid;
            return false;
        }

        if (probationMonths == AllowedProbationPeriodMonths)
        {
            if (values.ProbationStartDate is null)
            {
                field = HrValidation.Fields.ProbationStartDate;
                code = HrValidation.Codes.ProbationStartDateRequired;
                return false;
            }
        }
        else if (values.ProbationStartDate is not null)
        {
            field = HrValidation.Fields.ProbationStartDate;
            code = HrValidation.Codes.ProbationStartDateMustBeNull;
            return false;
        }

        var contractType = values.ContractType;
        var contractEnd = contractType == EmploymentContractType.FixedTerm ? values.ContractEndDate : null;
        var monthlyHours = contractType == EmploymentContractType.PartTime ? values.PartTimeMonthlyHours : null;

        if (contractType == EmploymentContractType.FixedTerm && contractEnd is null)
        {
            field = HrValidation.Fields.ContractEndDate;
            code = HrValidation.Codes.ContractEndDateRequired;
            return false;
        }

        if (contractEnd is { } contractEndDate && contractEndDate < StartDate)
        {
            field = HrValidation.Fields.ContractEndDate;
            code = HrValidation.Codes.ContractEndDateBeforeStart;
            return false;
        }

        if (contractType == EmploymentContractType.PartTime)
        {
            if (monthlyHours is null)
            {
                field = HrValidation.Fields.PartTimeMonthlyHours;
                code = HrValidation.Codes.PartTimeHoursRequired;
                return false;
            }

            if (monthlyHours.Value <= 0)
            {
                field = HrValidation.Fields.PartTimeMonthlyHours;
                code = HrValidation.Codes.PartTimeHoursInvalid;
                return false;
            }
        }

        if (values.IncentiveStartDate is { } incentiveStart
            && values.IncentiveEndDate is { } incentiveEnd
            && incentiveEnd < incentiveStart)
        {
            field = HrValidation.Fields.IncentiveEndDate;
            code = HrValidation.Codes.IncentiveRangeInvalid;
            return false;
        }

        if (values.WorkPermitStartDate is { } permitStart
            && values.WorkPermitEndDate is { } permitEnd
            && permitEnd < permitStart)
        {
            field = HrValidation.Fields.WorkPermitEndDate;
            code = HrValidation.Codes.WorkPermitRangeInvalid;
            return false;
        }

        WorkType = workType;
        ProbationPeriodMonths = probationMonths;
        ProbationStartDate = values.ProbationStartDate;
        RecruitmentSourceId = values.RecruitmentSourceId;
        ContractType = contractType;
        ContractEndDate = contractEnd;
        PartTimeMonthlyHours = monthlyHours;
        IskurStatus = values.IskurStatus;
        IncentiveStartDate = values.IncentiveStartDate;
        IncentiveEndDate = values.IncentiveEndDate;
        IskurWorkforceStatus = values.IskurWorkforceStatus;
        WorkPermitStartDate = values.WorkPermitStartDate;
        WorkPermitEndDate = values.WorkPermitEndDate;
        return true;
    }

    private bool startDateIsFuture(DateOnly today) => StartDate > today;
}

public sealed record EmploymentWorkforceTermsValues(
    EmploymentContractType? ContractType,
    DateOnly? ContractEndDate,
    decimal? PartTimeMonthlyHours,
    IskurStatus? IskurStatus,
    DateOnly? IncentiveStartDate,
    DateOnly? IncentiveEndDate,
    IskurWorkforceStatus? IskurWorkforceStatus,
    DateOnly? WorkPermitStartDate,
    DateOnly? WorkPermitEndDate,
    WorkType? WorkType = null,
    int? ProbationPeriodMonths = null,
    DateOnly? ProbationStartDate = null,
    Guid? RecruitmentSourceId = null);
