namespace HuGuWeb.Workforce.Domain;

public static class HrValidation
{
    public static class Codes
    {
        public const string TcknLength = "tckn-length";
        public const string TcknInvalid = "tckn-invalid";
        public const string YknFormat = "ykn-format";
        public const string PassportFormat = "passport-format";
        public const string IdentitySchemeRequired = "identity-scheme-required";
        public const string IdentityTooLong = "identity-too-long";
        public const string IdentityInvalid = "identity-invalid";
        public const string PhoneInvalid = "phone-invalid";
        public const string PhoneRequired = "phone-required";
        public const string EmailInvalid = "email-invalid";
        public const string EmailTooLong = "email-too-long";
        public const string BirthDateInvalid = "birth-date-invalid";
        public const string TextTooLong = "text-too-long";
        public const string GivenNameRequired = "given-name-required";
        public const string GivenNameTooLong = "given-name-too-long";
        public const string FamilyNameRequired = "family-name-required";
        public const string FamilyNameTooLong = "family-name-too-long";
        public const string PersonnelNumberRequired = "personnel-number-required";
        public const string PersonnelNumberTooLong = "personnel-number-too-long";
        public const string EmergencyNameRequired = "emergency-name-required";
        public const string EmergencyNameTooLong = "emergency-name-too-long";
        public const string EmergencyPrimaryMultiple = "emergency-primary-multiple";
        public const string DepartmentRequired = "department-required";
        public const string PositionRequired = "position-required";
        public const string StartDateRequired = "start-date-required";
    }

    public static class Fields
    {
        public const string GivenName = "givenName";
        public const string FamilyName = "familyName";
        public const string PersonnelNumber = "personnelNumber";
        public const string EmploymentStartDate = "employmentStartDate";
        public const string DepartmentId = "departmentId";
        public const string PositionId = "positionId";
        public const string NationalIdentityScheme = "nationalIdentityScheme";
        public const string NationalIdentityNumber = "nationalIdentityNumber";
        public const string Nationality = "nationality";
        public const string BirthDate = "birthDate";
        public const string BirthPlace = "birthPlace";
        public const string MobilePhone = "mobilePhone";
        public const string HomePhone = "homePhone";
        public const string Email = "email";
        public const string ResidenceAddress = "residenceAddress";
        public const string ResidenceCity = "residenceCity";
        public const string ResidenceDistrict = "residenceDistrict";
        public const string NotificationAddress = "notificationAddress";
        public const string HrNotes = "hrNotes";
        public const string EmergencyContacts = "emergencyContacts";

        public static string EmergencyName(int index) => $"emergencyContacts[{index}].name";

        public static string EmergencyRelationship(int index) => $"emergencyContacts[{index}].relationship";

        public static string EmergencyPhone(int index) => $"emergencyContacts[{index}].phone";
    }
}
