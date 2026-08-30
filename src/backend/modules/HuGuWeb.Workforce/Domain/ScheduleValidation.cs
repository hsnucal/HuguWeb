namespace HuGuWeb.Workforce.Domain;

public static class ScheduleValidation
{
    public static class Codes
    {
        public const string ShiftDefinitionNotFound = "shift-definition-not-found";
        public const string ShiftDefinitionCodeExists = "shift-definition-code-exists";
        public const string ShiftDefinitionCodeRequired = "shift-definition-code-required";
        public const string ShiftDefinitionCodeTooLong = "shift-definition-code-too-long";
        public const string ShiftDefinitionNameRequired = "shift-definition-name-required";
        public const string ShiftDefinitionNameTooLong = "shift-definition-name-too-long";
        public const string ShiftDefinitionInvalidTime = "shift-definition-invalid-time";
        public const string ShiftDefinitionInvalidBreak = "shift-definition-invalid-break";
        public const string ShiftDefinitionInactive = "shift-definition-inactive";
        public const string ShiftDefinitionSemanticFieldsLocked = "shift-definition-semantic-fields-locked";
        public const string ScheduleEmploymentNotCoveringDate = "schedule-employment-not-covering-date";
        public const string ScheduleAssignmentNotFound = "schedule-assignment-not-found";
        public const string ScheduleCrossPropertyShift = "schedule-cross-property-shift";
        public const string ScheduleInvalidKind = "schedule-invalid-kind";
        public const string ScheduleEntryConflict = "schedule-entry-conflict";
        public const string ScheduleNoteTooLong = "schedule-note-too-long";
        public const string ScheduleInvalidRange = "schedule-invalid-range";
        public const string ScheduleShiftDefinitionRequired = "schedule-shift-definition-required";
        public const string ScheduleShiftDefinitionMustBeNull = "schedule-shift-definition-must-be-null";
        public const string SchedulePropertyAccessDenied = "schedule-property-access-denied";
        public const string ScheduleWeekStartInvalid = "schedule-week-start-invalid";
        public const string ScheduleDepartmentFilterDenied = "schedule-department-filter-denied";
        public const string ScheduleBulkFailed = "schedule-bulk-failed";
        public const string ScheduleCopyWeekBlocked = "schedule-copy-week-blocked";
        public const string ScheduleCopyWeekEmpty = "schedule-copy-week-empty";
    }

    public static class Fields
    {
        public const string ShiftDefinitionId = "shiftDefinitionId";
        public const string Code = "code";
        public const string Name = "name";
        public const string StartLocalTime = "startLocalTime";
        public const string EndLocalTime = "endLocalTime";
        public const string EndsNextDay = "endsNextDay";
        public const string BreakMinutes = "breakMinutes";
        public const string Kind = "kind";
        public const string ScheduleDate = "scheduleDate";
        public const string Note = "note";
        public const string From = "from";
        public const string To = "to";
        public const string IsActive = "isActive";
        public const string WeekStart = "weekStart";
        public const string DepartmentId = "departmentId";
        public const string Operations = "operations";
    }
}
