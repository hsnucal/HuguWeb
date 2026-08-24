using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed record WorkforceError(
    string Code,
    string Title,
    string Detail,
    int StatusCode,
    IReadOnlyDictionary<string, string[]>? Errors = null)
{
    public static WorkforceError RequiredField(string field, string detail) =>
        new("invalid-request", "The request is invalid.", detail, 400);

    public static WorkforceError InvalidRequest(string code, string detail) =>
        new(code, "The request is invalid.", detail, 400);

    public static WorkforceError InvalidFields(
        string code,
        string detail,
        string field,
        string reason) =>
        new(
            code,
            "The request is invalid.",
            detail,
            400,
            new Dictionary<string, string[]> { [field] = [reason] });

    public static WorkforceError InvalidFields(
        string code,
        string detail,
        IReadOnlyDictionary<string, string[]> errors) =>
        new(code, "The request is invalid.", detail, 400, errors);

    public static string FieldForEmployeeCode(string? code) =>
        code switch
        {
            HrValidation.Codes.FamilyNameRequired or HrValidation.Codes.FamilyNameTooLong =>
                HrValidation.Fields.FamilyName,
            HrValidation.Codes.PersonnelNumberRequired or HrValidation.Codes.PersonnelNumberTooLong =>
                HrValidation.Fields.PersonnelNumber,
            _ => HrValidation.Fields.GivenName
        };

    public static WorkforceError NotFound(string code, string detail) =>
        new(code, "The requested resource was not found.", detail, 404);

    public static WorkforceError Conflict(string code, string title, string detail) =>
        new(code, title, detail, 409);

    public static WorkforceError PersonnelNumberInUse() =>
        Conflict(
            "personnel-number-in-use",
            "Personnel number is already in use.",
            "This personnel number already belongs to an employee in the organization, including former staff.") with
        {
            Errors = new Dictionary<string, string[]>
            {
                [HrValidation.Fields.PersonnelNumber] = ["personnel-number-in-use"]
            }
        };

    public static WorkforceError MultipleOpenEmployments() =>
        Conflict(
            "multiple-open-employments",
            "Employee has more than one current employment.",
            "An employee cannot have multiple simultaneous non-ended employments.");

    public static WorkforceError DepartmentNotFound() =>
        NotFound("department-not-found", "The department was not found.") with
        {
            Errors = new Dictionary<string, string[]>
            {
                [HrValidation.Fields.DepartmentId] = ["department-not-found"]
            }
        };

    public static WorkforceError PositionNotFound() =>
        NotFound("position-not-found", "The position was not found.") with
        {
            Errors = new Dictionary<string, string[]>
            {
                [HrValidation.Fields.PositionId] = ["position-not-found"]
            }
        };

    public static WorkforceError EmployeeNotFound() =>
        NotFound("employee-not-found", "The employee was not found.");

    public static WorkforceError DepartmentInactive() =>
        InvalidFields(
            "department-inactive",
            "An inactive department cannot receive a new assignment.",
            HrValidation.Fields.DepartmentId,
            "department-inactive");

    public static WorkforceError PositionInactive() =>
        InvalidFields(
            "position-inactive",
            "An inactive position cannot receive a new assignment.",
            HrValidation.Fields.PositionId,
            "position-inactive");

    public static WorkforceError PositionNotAvailableForDepartment() =>
        InvalidFields(
            "position-not-available-for-department",
            "The selected position cannot be used in this department.",
            HrValidation.Fields.PositionId,
            "position-not-available-for-department");

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

    public static WorkforceError Forbidden(string code, string detail) =>
        new(code, "Access denied.", detail, 403);

    public static WorkforceError SensitiveWriteForbidden() =>
        Forbidden(
            "sensitive-write-forbidden",
            "This account cannot change restricted HR fields.");

    public static WorkforceError NationalIdentityInUse() =>
        Conflict(
            "national-identity-in-use",
            "National identity is already in use.",
            "This national identity already belongs to an employee in the organization.") with
        {
            Errors = new Dictionary<string, string[]>
            {
                [HrValidation.Fields.NationalIdentityNumber] = ["national-identity-in-use"]
            }
        };

    public static WorkforceError PhotoNotFound() =>
        NotFound("photo-not-found", "The employee photo was not found.");

    public static WorkforceError InvalidPhoto(string detail) =>
        InvalidRequest("invalid-photo", detail);

    public static WorkforceError EmploymentNotFound() =>
        NotFound("employment-not-found", "The employment was not found.");

    public static WorkforceError SgkWorkplaceNotFound() =>
        NotFound("sgk-workplace-not-found", "The SGK workplace registration was not found.") with
        {
            Errors = new Dictionary<string, string[]>
            {
                [HrValidation.Fields.SgkWorkplaceRegistrationId] = ["sgk-workplace-not-found"]
            }
        };

    public static WorkforceError SgkWorkplaceInactive() =>
        InvalidFields(
            "sgk-workplace-inactive",
            "An inactive SGK workplace registration cannot be newly selected.",
            HrValidation.Fields.SgkWorkplaceRegistrationId,
            "sgk-workplace-inactive");

    public static WorkforceError SgkWorkplaceNotForProperty() =>
        InvalidFields(
            "sgk-workplace-not-for-property",
            "The SGK workplace registration does not belong to this employment's property.",
            HrValidation.Fields.SgkWorkplaceRegistrationId,
            "sgk-workplace-not-for-property");

    public static WorkforceError InvalidDocumentTypeCode() =>
        InvalidFields(
            "invalid-document-type-code",
            "The document type code is not a valid active lookup value.",
            HrValidation.Fields.DocumentTypeCode,
            "invalid-document-type-code");

    public static WorkforceError InvalidApplicableLawCode() =>
        InvalidFields(
            "invalid-applicable-law-code",
            "The applicable law code is not a valid active lookup value.",
            HrValidation.Fields.ApplicableLawCode,
            "invalid-applicable-law-code");

    public static WorkforceError InvalidInsuranceBranchCode() =>
        InvalidFields(
            "invalid-insurance-branch-code",
            "The insurance branch code is not a valid active lookup value.",
            HrValidation.Fields.InsuranceBranchCode,
            "invalid-insurance-branch-code");

    public static WorkforceError InvalidOccupationCode() =>
        InvalidFields(
            "invalid-occupation-code",
            "The occupation code is not a valid active catalogue value.",
            HrValidation.Fields.OccupationCode,
            "invalid-occupation-code");

    public static WorkforceError InvalidDutyCode() =>
        InvalidFields(
            "invalid-duty-code",
            "The duty code is not a valid active lookup value.",
            HrValidation.Fields.DutyCode,
            "invalid-duty-code");

    public static WorkforceError EmploymentPropertyUnresolved() =>
        InvalidRequest(
            "employment-property-unresolved",
            "This employment does not have an unambiguous property context for SGK workplace selection.");

    public static WorkforceError InvalidSgkWorkplace(string code, string field, string detail) =>
        InvalidFields(code, detail, field, code);
}
