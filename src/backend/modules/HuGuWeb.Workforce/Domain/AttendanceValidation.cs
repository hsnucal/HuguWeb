namespace HuGuWeb.Workforce.Domain;

public static class AttendanceValidation
{
    public static class Codes
    {
        public const string AttendanceInvalidMonth = "attendance-invalid-month";
        public const string AttendanceOutsideEmployment = "attendance-outside-employment";
        public const string AttendanceCorrectionReasonRequired = "attendance-correction-reason-required";
        public const string AttendanceCorrectionReasonTooLong = "attendance-correction-reason-too-long";
        public const string AttendanceCorrectionKindInvalid = "attendance-correction-kind-invalid";
        public const string AttendanceEmploymentNotFound = "attendance-employment-not-found";
        public const string AttendanceDepartmentScopeDenied = "attendance-department-scope-denied";
        public const string AttendancePropertyAccessDenied = "attendance-property-access-denied";
        public const string AttendanceAssignmentNotFound = "attendance-assignment-not-found";
        public const string AttendanceDepartmentFilterDenied = "attendance-department-filter-denied";
    }

    public static class Fields
    {
        public const string Year = "year";
        public const string Month = "month";
        public const string LocalDate = "localDate";
        public const string Kind = "kind";
        public const string Reason = "reason";
        public const string EmploymentId = "employmentId";
        public const string DepartmentId = "departmentId";
    }
}
