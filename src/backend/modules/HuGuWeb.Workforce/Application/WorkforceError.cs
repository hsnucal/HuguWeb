namespace HuGuWeb.Workforce.Application;

public sealed record WorkforceError(string Code, string Title, string Detail, int StatusCode)
{
    public static WorkforceError RequiredField(string field, string detail) =>
        new("invalid-request", "The request is invalid.", detail, 400);

    public static WorkforceError InvalidRequest(string code, string detail) =>
        new(code, "The request is invalid.", detail, 400);

    public static WorkforceError NotFound(string code, string detail) =>
        new(code, "The requested resource was not found.", detail, 404);

    public static WorkforceError Conflict(string code, string title, string detail) =>
        new(code, title, detail, 409);

    public static WorkforceError PersonnelNumberInUse() =>
        Conflict(
            "personnel-number-in-use",
            "Personnel number is already in use.",
            "This personnel number already belongs to an employee in the organization, including former staff.");

    public static WorkforceError MultipleOpenEmployments() =>
        Conflict(
            "multiple-open-employments",
            "Employee has more than one current employment.",
            "An employee cannot have multiple simultaneous non-ended employments.");

    public static WorkforceError DepartmentNotFound() =>
        NotFound("department-not-found", "The department was not found.");

    public static WorkforceError PositionNotFound() =>
        NotFound("position-not-found", "The position was not found.");

    public static WorkforceError EmployeeNotFound() =>
        NotFound("employee-not-found", "The employee was not found.");

    public static WorkforceError DepartmentInactive() =>
        InvalidRequest("department-inactive", "An inactive department cannot receive a new assignment.");

    public static WorkforceError PositionInactive() =>
        InvalidRequest("position-inactive", "An inactive position cannot receive a new assignment.");

    public static WorkforceError EmploymentEnded() =>
        InvalidRequest("employment-ended", "An ended employment cannot receive a new assignment or be changed.");

    public static WorkforceError NoCurrentEmployment() =>
        InvalidRequest("no-current-employment", "The employee does not have a current employment.");

    public static WorkforceError InvalidEmploymentPeriod() =>
        InvalidRequest("invalid-employment-period", "Employment end date must be on or after the start date.");

    public static WorkforceError InvalidAssignmentPeriod() =>
        InvalidRequest("invalid-assignment-period", "Assignment end date must be on or after the start date.");

    public static WorkforceError AssignmentOutsideEmployment() =>
        InvalidRequest(
            "assignment-outside-employment",
            "A primary assignment must stay within the employment period.");

    public static WorkforceError OverlappingPrimaryAssignment() =>
        InvalidRequest(
            "overlapping-primary-assignment",
            "Primary assignments cannot overlap. The previous primary must end the day before the new primary starts.");

    public static WorkforceError InvalidTransferDate() =>
        InvalidRequest(
            "invalid-transfer-date",
            "The transfer date would invert an assignment period or overlap another primary assignment.");

    public static WorkforceError SameAssignment() =>
        InvalidRequest(
            "same-assignment",
            "The employee is already assigned to this department and position on the effective date.");

    public static WorkforceError WorkplaceNotConfigured() =>
        new(
            "workplace-not-configured",
            "Workplace is not configured.",
            "Organization and Property must be configured for workforce operations.",
            500);
}
