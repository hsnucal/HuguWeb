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
        public const string RegistrationNumberRequired = "registration-number-required";
        public const string RegistrationNumberTooLong = "registration-number-too-long";
        public const string DisplayNameTooLong = "display-name-too-long";
        public const string InvalidDocumentTypeCode = "invalid-document-type-code";
        public const string InvalidApplicableLawCode = "invalid-applicable-law-code";
        public const string InvalidInsuranceBranchCode = "invalid-insurance-branch-code";
        public const string InvalidOccupationCode = "invalid-occupation-code";
        public const string SgkWorkplaceNotFound = "sgk-workplace-not-found";
        public const string SgkWorkplaceInactive = "sgk-workplace-inactive";
        public const string SgkWorkplaceNotForProperty = "sgk-workplace-not-for-property";
        public const string EmploymentPropertyUnresolved = "employment-property-unresolved";
        public const string InvalidDutyCode = "invalid-duty-code";
        public const string InvalidNationality = "invalid-nationality";
        public const string MilitaryExemptionReasonRequired = "military-exemption-reason-required";
        public const string MilitaryDefermentReasonRequired = "military-deferment-reason-required";
        public const string ContractEndDateRequired = "contract-end-date-required";
        public const string PartTimeHoursRequired = "part-time-hours-required";
        public const string PartTimeHoursInvalid = "part-time-hours-invalid";
        public const string IncentiveRangeInvalid = "incentive-range-invalid";
        public const string WorkPermitRangeInvalid = "work-permit-range-invalid";
        public const string BesRateInvalid = "bes-rate-invalid";
        public const string BesExtraAmountInvalid = "bes-extra-amount-invalid";
        public const string KepInvalid = "kep-invalid";
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
        public const string SgkWorkplaceRegistrationId = "sgkWorkplaceRegistrationId";
        public const string DocumentTypeCode = "documentTypeCode";
        public const string ApplicableLawCode = "applicableLawCode";
        public const string InsuranceBranchCode = "insuranceBranchCode";
        public const string OccupationCode = "occupationCode";
        public const string DutyCode = "dutyCode";
        public const string ContractType = "contractType";
        public const string ContractEndDate = "contractEndDate";
        public const string PartTimeMonthlyHours = "partTimeMonthlyHours";
        public const string IskurStatus = "iskurStatus";
        public const string IncentiveStartDate = "incentiveStartDate";
        public const string IncentiveEndDate = "incentiveEndDate";
        public const string IskurWorkforceStatus = "iskurWorkforceStatus";
        public const string BesDeductionEnabled = "besDeductionEnabled";
        public const string BesRatePercent = "besRatePercent";
        public const string BesExtraAmount = "besExtraAmount";
        public const string DrivingLicenceCategory = "drivingLicenceCategory";
        public const string MilitaryServiceStatus = "militaryServiceStatus";
        public const string MilitaryExemptionReason = "militaryExemptionReason";
        public const string MilitaryDefermentReason = "militaryDefermentReason";
        public const string KepAddress = "kepAddress";
        public const string WorkPermitStartDate = "workPermitStartDate";
        public const string WorkPermitEndDate = "workPermitEndDate";
        public const string EducationDescription = "educationDescription";
        public const string SchoolName = "schoolName";
        public const string GraduationDate = "graduationDate";
        public const string ForeignLanguage = "foreignLanguage";
        public const string ArgeProjectCode = "argeProjectCode";
        public const string RegistrationNumber = "registrationNumber";
        public const string DisplayName = "displayName";
        public const string PaymentIban = "paymentIban";
        public const string PaymentBankName = "paymentBankName";

        public static string EmergencyName(int index) => $"emergencyContacts[{index}].name";

        public static string EmergencyRelationship(int index) => $"emergencyContacts[{index}].relationship";

        public static string EmergencyPhone(int index) => $"emergencyContacts[{index}].phone";
    }
}
