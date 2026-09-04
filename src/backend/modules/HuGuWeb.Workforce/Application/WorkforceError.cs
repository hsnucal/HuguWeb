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

    public static WorkforceError TerminationReasonRequired() =>
        InvalidFields(
            HrValidation.Codes.TerminationReasonRequired,
            "A termination reason is required when ending employment.",
            HrValidation.Fields.TerminationReason,
            HrValidation.Codes.TerminationReasonRequired);

    public static WorkforceError InvalidTerminationReason() =>
        InvalidFields(
            HrValidation.Codes.InvalidTerminationReason,
            "The termination reason is not a recognised HuGu employment exit reason.",
            HrValidation.Fields.TerminationReason,
            HrValidation.Codes.InvalidTerminationReason);

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

    public static WorkforceError MovementInvalidType() =>
        InvalidFields(
            MovementValidation.Codes.InvalidType,
            "The movement type is not supported.",
            MovementValidation.Fields.Type,
            MovementValidation.Codes.InvalidType);

    public static WorkforceError MovementField(string field, string code, string detail) =>
        InvalidFields(code, detail, field, code);

    public static WorkforceError MovementEmploymentNotFound() =>
        NotFound(MovementValidation.Codes.EmploymentNotFound, "The employment was not found.");

    public static WorkforceError MovementNotFound() =>
        NotFound(MovementValidation.Codes.NotFound, "The personnel movement was not found.");

    public static WorkforceError MovementSameTarget() =>
        InvalidRequest(
            MovementValidation.Codes.SameTarget,
            "The movement target is the same as the current assignment or manager.");

    public static WorkforceError MovementPositionNotApplicable() =>
        InvalidFields(
            MovementValidation.Codes.PositionNotApplicable,
            "The selected position cannot be used in the target department.",
            MovementValidation.Fields.TargetPositionId,
            MovementValidation.Codes.PositionNotApplicable);

    public static WorkforceError MovementPropertyAccessDenied() =>
        Forbidden(
            MovementValidation.Codes.PropertyAccessDenied,
            "Movement manage permission does not cover the required property.");

    public static WorkforceError MovementCrossOrganizationNotSupported() =>
        InvalidRequest(
            MovementValidation.Codes.CrossOrganizationNotSupported,
            "Cross-organization transfer is not a personnel movement. End employment and hire in the destination organization.");

    public static WorkforceError MovementPendingLeaveConflict() =>
        Conflict(
            MovementValidation.Codes.PendingLeaveConflict,
            "Pending leave request conflict.",
            "A pending leave request spans the movement effective date. Withdraw or complete the leave request before changing assignment.");

    public static WorkforceError MovementScheduleConflict() =>
        Conflict(
            MovementValidation.Codes.ScheduleConflict,
            "Schedule conflict.",
            "Future schedule entries or attendance corrections reference the current assignment on or after the movement date. Adjust the schedule before moving.");

    public static WorkforceError MovementNotCancellable() =>
        InvalidRequest(
            MovementValidation.Codes.NotCancellable,
            "This movement cannot be cancelled.");

    public static WorkforceError MovementAlreadyEffective() =>
        InvalidRequest(
            MovementValidation.Codes.AlreadyEffective,
            "An effective movement cannot be cancelled. Record a new movement to reverse or correct it.");

    public static WorkforceError MovementAlreadyCancelled() =>
        Conflict(
            MovementValidation.Codes.AlreadyCancelled,
            "Movement is already cancelled.",
            "A cancelled movement cannot be cancelled again.");

    public static WorkforceError ReportingLineSelfManager() =>
        InvalidRequest(
            MovementValidation.Codes.SelfManager,
            "An employment cannot report to itself.");

    public static WorkforceError ReportingLineCycle() =>
        InvalidRequest(
            MovementValidation.Codes.Cycle,
            "This manager change would create a reporting-line cycle.");

    public static WorkforceError ReportingLineOverlap() =>
        InvalidRequest(
            MovementValidation.Codes.Overlap,
            "Direct manager ranges cannot overlap. The previous line must end the day before the new line starts.");

    public static WorkforceError ReportingLineManagerNotFound() =>
        NotFound(MovementValidation.Codes.ManagerNotFound, "The manager employment was not found.");

    public static WorkforceError ReportingLineOrganizationMismatch() =>
        InvalidRequest(
            MovementValidation.Codes.OrganizationMismatch,
            "Manager and subordinate must belong to the same organization.");

    public static WorkforceError MovementManagerLevelInvalid() =>
        InvalidFields(
            MovementValidation.Codes.ManagerLevelInvalid,
            "The selected manager must be at the subordinate's next organizational level.",
            MovementValidation.Fields.TargetManagerEmploymentId,
            MovementValidation.Codes.ManagerLevelInvalid);

    public static WorkforceError MovementManagerCannotManage() =>
        InvalidFields(
            MovementValidation.Codes.ManagerCannotManage,
            "The selected manager's position is not allowed to manage employees.",
            MovementValidation.Fields.TargetManagerEmploymentId,
            MovementValidation.Codes.ManagerCannotManage);

    public static WorkforceError MovementTargetNotPromotion() =>
        InvalidFields(
            MovementValidation.Codes.TargetNotPromotion,
            "The promotion target must be at a higher organizational level than the current position.",
            MovementValidation.Fields.TargetPositionId,
            MovementValidation.Codes.TargetNotPromotion);

    public static WorkforceError WorkplaceNotConfigured() =>
        new(
            "workplace-not-configured",
            "Workplace is not configured.",
            "Organization must be configured for workforce operations.",
            500);

    public static WorkforceError PropertyContextRequired() =>
        new(
            "property-context-required",
            "Property context is required.",
            "Select an explicit Property before performing this operation.",
            400);

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

    public static WorkforceError PersonnelImportInvalidFile(string detail) =>
        InvalidRequest("personnel-import-invalid-file", detail);

    public static WorkforceError PersonnelImportTooLarge(string detail) =>
        InvalidRequest("personnel-import-too-large", detail);

    public static WorkforceError PersonnelImportPreviewExpired() =>
        InvalidRequest("personnel-import-preview-expired", "Import preview has expired. Upload the file again.");

    public static WorkforceError PersonnelImportPreviewInvalid() =>
        InvalidRequest("personnel-import-preview-invalid", "Import preview is invalid or incomplete.");

    public static WorkforceError PersonnelImportPreviewForbidden() =>
        InvalidRequest("personnel-import-preview-forbidden", "Import preview belongs to another user or property context.");

    public static WorkforceError PersonnelImportFailed(string detail) =>
        InvalidRequest("personnel-import-failed", detail);

    public static WorkforceError PersonnelImportRowInvalid(string detail) =>
        InvalidRequest("personnel-import-row-invalid", detail);

    public static WorkforceError PaymentProfileInvalidIban() =>
        InvalidFields(
            "payment-profile-invalid-iban",
            "IBAN is invalid.",
            HrValidation.Fields.PaymentIban,
            "payment-profile-invalid-iban");

    public static WorkforceError LeaveTypeNotFound() =>
        NotFound(LeaveValidation.Codes.LeaveTypeNotFound, "The leave type was not found.") with
        {
            Errors = new Dictionary<string, string[]>
            {
                [LeaveValidation.Fields.LeaveTypeId] = [LeaveValidation.Codes.LeaveTypeNotFound]
            }
        };

    public static WorkforceError LeaveTypeInactive() =>
        InvalidFields(
            LeaveValidation.Codes.LeaveTypeInactive,
            "An inactive leave type cannot be used for a new movement or record.",
            LeaveValidation.Fields.LeaveTypeId,
            LeaveValidation.Codes.LeaveTypeInactive);

    public static WorkforceError LeaveTypeCodeConflict() =>
        Conflict(
            LeaveValidation.Codes.LeaveTypeCodeConflict,
            "Leave type code is already in use.",
            "This leave type code already exists in the organization, including inactive types.") with
        {
            Errors = new Dictionary<string, string[]>
            {
                [LeaveValidation.Fields.Code] = [LeaveValidation.Codes.LeaveTypeCodeConflict]
            }
        };

    public static WorkforceError LeaveValidationField(string field, string code, string detail) =>
        InvalidFields(code, detail, field, code);

    public static WorkforceError LeaveEntitlementBalanceNotSupported() =>
        InvalidFields(
            LeaveValidation.Codes.LeaveEntitlementBalanceNotSupported,
            "Entitlement movements are only valid for balance-tracked leave types.",
            LeaveValidation.Fields.LeaveTypeId,
            LeaveValidation.Codes.LeaveEntitlementBalanceNotSupported);

    public static WorkforceError LeaveDateOutsideEmployment() =>
        InvalidRequest(
            LeaveValidation.Codes.LeaveDateOutsideEmployment,
            "Leave dates must fall inside the employment period.");

    public static WorkforceError LeaveOverlap() =>
        Conflict(
            LeaveValidation.Codes.LeaveOverlap,
            "Leave dates overlap an existing record.",
            "Another recorded leave for this employment covers one or more of these dates.");

    public static WorkforceError LeaveRecordNotFound() =>
        NotFound(LeaveValidation.Codes.LeaveRecordNotFound, "The leave record was not found.");

    public static WorkforceError LeaveAlreadyCancelled() =>
        Conflict(
            LeaveValidation.Codes.LeaveAlreadyCancelled,
            "Leave record is already cancelled.",
            "A cancelled leave record cannot be cancelled again.");

    public static WorkforceError LeaveRequestNotFound() =>
        NotFound(LeaveValidation.Codes.LeaveRequestNotFound, "The leave request was not found.");

    public static WorkforceError LeaveRequestDateOutsideEmployment() =>
        InvalidRequest(
            LeaveValidation.Codes.LeaveRequestDateOutsideEmployment,
            "Leave request dates must fall inside the employment period.");

    public static WorkforceError LeaveRequestAssignmentNotFound() =>
        InvalidRequest(
            LeaveValidation.Codes.LeaveRequestAssignmentNotFound,
            "No primary assignment covers the leave request start date.");

    public static WorkforceError LeaveRequestCrossAssignmentRange() =>
        InvalidRequest(
            LeaveValidation.Codes.LeaveRequestCrossAssignmentRange,
            "The leave request date range must stay inside a single primary assignment. Submit separate requests.");

    public static WorkforceError LeaveRequestTypeInactive() =>
        InvalidFields(
            LeaveValidation.Codes.LeaveRequestTypeInactive,
            "An inactive leave type cannot be used for a new leave request.",
            LeaveValidation.Fields.LeaveTypeId,
            LeaveValidation.Codes.LeaveRequestTypeInactive);

    public static WorkforceError LeaveRequestOverlap() =>
        Conflict(
            LeaveValidation.Codes.LeaveRequestOverlap,
            "Leave request dates overlap an existing request or record.",
            "Another pending or approved leave request, or a recorded leave, covers one or more of these dates.");

    public static WorkforceError LeaveRequestNotPending() =>
        Conflict(
            LeaveValidation.Codes.LeaveRequestNotPending,
            "Leave request is not pending.",
            "This action is only valid for a pending leave request.");

    public static WorkforceError LeaveRequestInvalidApprovalStage() =>
        Conflict(
            LeaveValidation.Codes.LeaveRequestInvalidApprovalStage,
            "Leave request is at the wrong approval stage.",
            "This action is not valid for the current approval stage.");

    public static WorkforceError LeaveRequestAlreadyFinalized() =>
        Conflict(
            LeaveValidation.Codes.LeaveRequestAlreadyFinalized,
            "Leave request is already finalized.",
            "An approved, rejected, or cancelled leave request cannot be changed.");

    public static WorkforceError LeaveRequestRecordConflict() =>
        Conflict(
            LeaveValidation.Codes.LeaveRequestRecordConflict,
            "Leave request record conflict.",
            "A leave record for this request already exists or cannot be linked for cancellation.");

    public static WorkforceError LeaveRequestAccountLinkRequired() =>
        Forbidden(
            LeaveValidation.Codes.LeaveRequestAccountLinkRequired,
            "An employee account link is required for leave self-service.");

    public static WorkforceError LeaveRequestCurrentEmploymentNotFound() =>
        InvalidRequest(
            LeaveValidation.Codes.LeaveRequestCurrentEmploymentNotFound,
            "No current open employment is available for leave self-service.");

    public static WorkforceError LeaveRequestNotOwned() =>
        NotFound(LeaveValidation.Codes.LeaveRequestNotOwned, "The leave request was not found.");

    public static WorkforceError LeaveRequestDepartmentAccessDenied() =>
        Forbidden(
            LeaveValidation.Codes.LeaveRequestDepartmentAccessDenied,
            "Department scope does not allow this leave request.");

    public static WorkforceError LeaveRequestApprovalPermissionDenied() =>
        Forbidden(
            LeaveValidation.Codes.LeaveRequestApprovalPermissionDenied,
            "Leave approval permission is required for this action.");

    public static WorkforceError LeaveRequestInvalidFinalAmount() =>
        InvalidFields(
            LeaveValidation.Codes.LeaveRequestInvalidFinalAmount,
            "Final leave amount must be greater than zero and a multiple of 0.5 days.",
            LeaveValidation.Fields.FinalAmount,
            LeaveValidation.Codes.LeaveRequestInvalidFinalAmount);

    public static WorkforceError ShiftDefinitionNotFound() =>
        NotFound(ScheduleValidation.Codes.ShiftDefinitionNotFound, "The shift definition was not found.") with
        {
            Errors = new Dictionary<string, string[]>
            {
                [ScheduleValidation.Fields.ShiftDefinitionId] = [ScheduleValidation.Codes.ShiftDefinitionNotFound]
            }
        };

    public static WorkforceError ShiftDefinitionCodeExists() =>
        Conflict(
            ScheduleValidation.Codes.ShiftDefinitionCodeExists,
            "Shift definition code is already in use.",
            "This shift definition code already exists for the property, including inactive definitions.") with
        {
            Errors = new Dictionary<string, string[]>
            {
                [ScheduleValidation.Fields.Code] = [ScheduleValidation.Codes.ShiftDefinitionCodeExists]
            }
        };

    public static WorkforceError ShiftDefinitionInactive() =>
        InvalidFields(
            ScheduleValidation.Codes.ShiftDefinitionInactive,
            "An inactive shift definition cannot be newly assigned.",
            ScheduleValidation.Fields.ShiftDefinitionId,
            ScheduleValidation.Codes.ShiftDefinitionInactive);

    public static WorkforceError ScheduleValidationField(string field, string code, string detail) =>
        InvalidFields(code, detail, field, code);

    public static WorkforceError ScheduleEmploymentNotCoveringDate() =>
        InvalidFields(
            ScheduleValidation.Codes.ScheduleEmploymentNotCoveringDate,
            "No employment covers the requested schedule date.",
            ScheduleValidation.Fields.ScheduleDate,
            ScheduleValidation.Codes.ScheduleEmploymentNotCoveringDate);

    public static WorkforceError ScheduleAssignmentNotFound() =>
        InvalidFields(
            ScheduleValidation.Codes.ScheduleAssignmentNotFound,
            "No primary assignment covers the requested schedule date.",
            ScheduleValidation.Fields.ScheduleDate,
            ScheduleValidation.Codes.ScheduleAssignmentNotFound);

    public static WorkforceError ScheduleCrossPropertyShift() =>
        InvalidFields(
            ScheduleValidation.Codes.ScheduleCrossPropertyShift,
            "The shift definition belongs to a different property than the assignment on this date.",
            ScheduleValidation.Fields.ShiftDefinitionId,
            ScheduleValidation.Codes.ScheduleCrossPropertyShift);

    public static WorkforceError SchedulePropertyAccessDenied() =>
        Forbidden(
            ScheduleValidation.Codes.SchedulePropertyAccessDenied,
            "This schedule date belongs to a property outside the current workplace scope.");

    public static WorkforceError ScheduleEntryConflict() =>
        Conflict(
            ScheduleValidation.Codes.ScheduleEntryConflict,
            "Schedule entry conflict.",
            "An authoritative schedule entry already exists for this employment and date.");

    public static WorkforceError ScheduleBulkOperationFailed(
        int operationIndex,
        Guid employeeId,
        DateOnly scheduleDate,
        WorkforceError inner) =>
        new(
            ScheduleValidation.Codes.ScheduleBulkFailed,
            "Bulk schedule operation failed.",
            $"Operation {operationIndex} for employee {employeeId:D} on {scheduleDate:yyyy-MM-dd} failed: {inner.Detail}",
            inner.StatusCode,
            MergeBulkErrors(operationIndex, employeeId, scheduleDate, inner.Errors, inner.Code));

    public static WorkforceError AttendanceValidationField(string field, string code, string detail) =>
        InvalidFields(code, detail, field, code);

    public static WorkforceError AttendanceEmploymentNotFound() =>
        NotFound(
            AttendanceValidation.Codes.AttendanceEmploymentNotFound,
            "The employment was not found.");

    public static WorkforceError AttendanceOutsideEmployment() =>
        InvalidFields(
            AttendanceValidation.Codes.AttendanceOutsideEmployment,
            "The attendance date is outside the employment period.",
            AttendanceValidation.Fields.LocalDate,
            AttendanceValidation.Codes.AttendanceOutsideEmployment);

    public static WorkforceError AttendanceAssignmentNotFound() =>
        InvalidFields(
            AttendanceValidation.Codes.AttendanceAssignmentNotFound,
            "No primary assignment covers the requested attendance date.",
            AttendanceValidation.Fields.LocalDate,
            AttendanceValidation.Codes.AttendanceAssignmentNotFound);

    public static WorkforceError AttendancePropertyAccessDenied() =>
        Forbidden(
            AttendanceValidation.Codes.AttendancePropertyAccessDenied,
            "This attendance date belongs to a property outside the current workplace scope.");

    public static WorkforceError AttendanceDepartmentScopeDenied() =>
        Forbidden(
            AttendanceValidation.Codes.AttendanceDepartmentScopeDenied,
            "Department scope does not allow this attendance record.");

    public static WorkforceError ScheduleCopyWeekBlocked(CopyScheduleWeekPreviewDto preview) =>
        InvalidRequest(
            ScheduleValidation.Codes.ScheduleCopyWeekBlocked,
            $"{preview.InvalidCount} target operation(s) cannot be applied. Copy was not started.") with
        {
            Errors = new Dictionary<string, string[]>
            {
                [ScheduleValidation.Fields.Operations] =
                [
                    .. preview.Invalid.Select(item =>
                        $"{item.EmployeeId:D}|{item.TargetDate:yyyy-MM-dd}|{item.Code}")
                ]
            }
        };

    private static IReadOnlyDictionary<string, string[]> MergeBulkErrors(
        int operationIndex,
        Guid employeeId,
        DateOnly scheduleDate,
        IReadOnlyDictionary<string, string[]>? innerErrors,
        string innerCode)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["operationIndex"] = [operationIndex.ToString()],
            ["employeeId"] = [employeeId.ToString("D")],
            [ScheduleValidation.Fields.ScheduleDate] = [scheduleDate.ToString("yyyy-MM-dd")],
            ["reason"] = [innerCode]
        };

        if (innerErrors is not null)
        {
            foreach (var (key, values) in innerErrors)
            {
                errors[key] = values.ToArray();
            }
        }

        return errors;
    }
}
