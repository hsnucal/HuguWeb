namespace HuGuWeb.Workforce.Domain;

public static class MovementValidation
{
    public static class Codes
    {
        public const string InvalidType = "movement-invalid-type";
        public const string ReasonRequired = "movement-reason-required";
        public const string ReasonTooLong = "movement-reason-too-long";
        public const string NoteTooLong = "movement-note-too-long";
        public const string EffectiveDateInvalid = "movement-effective-date-invalid";
        public const string EmploymentNotFound = "movement-employment-not-found";
        public const string AssignmentNotFound = "movement-assignment-not-found";
        public const string SameTarget = "movement-same-target";
        public const string PositionNotApplicable = "movement-position-not-applicable";
        public const string PropertyAccessDenied = "movement-property-access-denied";
        public const string CrossOrganizationNotSupported = "movement-cross-organization-not-supported";
        public const string PendingLeaveConflict = "movement-pending-leave-conflict";
        public const string ScheduleConflict = "movement-schedule-conflict";
        public const string NotCancellable = "movement-not-cancellable";
        public const string AlreadyEffective = "movement-already-effective";
        public const string AlreadyCancelled = "movement-already-cancelled";
        public const string NotFound = "movement-not-found";
        public const string TargetPositionRequired = "movement-target-position-required";
        public const string TargetDepartmentRequired = "movement-target-department-required";
        public const string TargetPropertyRequired = "movement-target-property-required";
        public const string SelfManager = "reporting-line-self-manager";
        public const string Cycle = "reporting-line-cycle";
        public const string Overlap = "reporting-line-overlap";
        public const string ManagerNotFound = "reporting-line-manager-not-found";
        public const string OrganizationMismatch = "reporting-line-organization-mismatch";
        public const string CancellationReasonRequired = "movement-cancellation-reason-required";
        public const string CancellationReasonTooLong = "movement-cancellation-reason-too-long";
    }

    public static class Fields
    {
        public const string Type = "type";
        public const string Reason = "reason";
        public const string Note = "note";
        public const string EffectiveDate = "effectiveDate";
        public const string EmploymentId = "employmentId";
        public const string TargetPropertyId = "targetPropertyId";
        public const string TargetDepartmentId = "targetDepartmentId";
        public const string TargetPositionId = "targetPositionId";
        public const string TargetManagerEmploymentId = "targetManagerEmploymentId";
        public const string CancellationReason = "cancellationReason";
    }
}
