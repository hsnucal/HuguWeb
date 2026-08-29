namespace HuGuWeb.Workforce.Domain;

public static class LeaveValidation
{
    public static class Codes
    {
        public const string LeaveTypeNotFound = "leave-type-not-found";
        public const string LeaveTypeInactive = "leave-type-inactive";
        public const string LeaveTypeCodeConflict = "leave-type-code-conflict";
        public const string LeaveTypeCodeRequired = "leave-type-code-required";
        public const string LeaveTypeCodeTooLong = "leave-type-code-too-long";
        public const string LeaveTypeNameRequired = "leave-type-name-required";
        public const string LeaveTypeNameTooLong = "leave-type-name-too-long";
        public const string LeaveTypeHasHistory = "leave-type-has-history";
        public const string LeaveTypeSystemImmutable = "leave-type-system-immutable";
        public const string LeaveEntitlementInvalidAmount = "leave-entitlement-invalid-amount";
        public const string LeaveEntitlementBalanceNotSupported = "leave-entitlement-balance-not-supported";
        public const string LeaveEntitlementNoteRequired = "leave-entitlement-note-required";
        public const string LeaveEntitlementInvalidSource = "leave-entitlement-invalid-source";
        public const string LeaveDateOutsideEmployment = "leave-date-outside-employment";
        public const string LeaveInvalidDateRange = "leave-invalid-date-range";
        public const string LeaveInvalidAmount = "leave-invalid-amount";
        public const string LeaveOverlap = "leave-overlap";
        public const string LeaveRecordNotFound = "leave-record-not-found";
        public const string LeaveAlreadyCancelled = "leave-already-cancelled";
        public const string LeaveCancellationReasonRequired = "leave-cancellation-reason-required";
        public const string LeaveNoteTooLong = "leave-note-too-long";
    }

    public static class Fields
    {
        public const string LeaveTypeId = "leaveTypeId";
        public const string Code = "code";
        public const string Name = "name";
        public const string TracksBalance = "tracksBalance";
        public const string EffectiveDate = "effectiveDate";
        public const string Amount = "amount";
        public const string Source = "source";
        public const string StartDate = "startDate";
        public const string EndDate = "endDate";
        public const string Note = "note";
        public const string CancellationReason = "cancellationReason";
    }
}
