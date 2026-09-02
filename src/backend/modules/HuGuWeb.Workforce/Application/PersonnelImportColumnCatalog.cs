namespace HuGuWeb.Workforce.Application;

public sealed record PersonnelImportColumn(
    string Id,
    string Header,
    bool Required,
    bool Sensitive,
    bool WrapText,
    IReadOnlyList<string> Aliases,
    IReadOnlyList<string>? ListValues = null);

public sealed record PersonnelImportCodeName(string? Code, string Name);

public sealed record PersonnelImportTemplateContext(
    IReadOnlyList<PersonnelImportCodeName> Departments,
    IReadOnlyList<PersonnelImportCodeName> Positions)
{
    public static PersonnelImportTemplateContext Empty { get; } = new([], []);
}

public static class PersonnelImportColumnCatalog
{
    public const string WorkbookVersion = "hugu-personnel-import-v2";
    public const string BrandFill = "#862A51";
    public const double MinColumnWidth = 10d;
    public const double MaxColumnWidth = 48d;

    public static IReadOnlyList<PersonnelImportColumn> Columns { get; } =
    [
        Col(Ids.PersonnelNumber, "Sicil No", false, false, false, ["Personnel Number", "Табельный номер"]),
        Col(Ids.GivenName, "Ad", true, false, false, ["Given Name", "Имя"]),
        Col(Ids.FamilyName, "Soyad", true, false, false, ["Family Name", "Фамилия"]),
        Col(Ids.DepartmentCode, "Departman Kodu", true, false, false, ["Department Code", "Код отдела"]),
        Col(Ids.Department, "Departman Adı", false, false, false, ["Departman", "Department", "Отдел"]),
        Col(Ids.PositionCode, "Pozisyon Kodu", true, false, false, ["Position Code", "Код должности"]),
        Col(Ids.Position, "Pozisyon Adı", false, false, false, ["Pozisyon", "Position", "Должность"]),
        Col(Ids.EmploymentStartDate, "İşe Giriş Tarihi", true, false, false, ["Employment Start Date", "Дата приёма"]),
        Col(Ids.MobilePhone, "Cep Telefonu", false, false, false, ["Mobile Phone", "Мобильный телефон"]),
        Col(Ids.HomePhone, "Ev Telefonu", false, false, false, ["Home Phone", "Домашний телефон"]),
        Col(Ids.Email, "E-posta", false, false, false, ["Email", "Электронная почта"]),
        Col(Ids.ResidenceCity, "İkamet İl", false, true, false, ["Residence City", "Город проживания"]),
        Col(Ids.ResidenceDistrict, "İkamet İlçe", false, true, false, ["Residence District", "Район проживания"]),
        Col(Ids.ResidenceAddress, "İkamet Adresi", false, true, true, ["Residence Address", "Адрес проживания"]),
        Col(Ids.NotificationAddress, "Bildirim Adresi", false, true, true, ["Notification Address", "Адрес уведомлений"]),
        Col(Ids.NationalIdentityScheme, "Kimlik Türü", false, true, false, ["Kimlik Şeması", "Identity Scheme", "Тип документа"],
            ["Tckn", "Ykn", "Passport", "Other"]),
        Col(Ids.NationalIdentityNumber, "Kimlik Numarası", false, true, false, ["Kimlik No", "Identity Number", "Номер документа"]),
        Col(Ids.BirthDate, "Doğum Tarihi", false, false, false, ["Birth Date", "Дата рождения"]),
        Col(Ids.BirthPlace, "Doğum Yeri", false, false, false, ["Birth Place", "Место рождения"]),
        Col(Ids.Gender, "Cinsiyet", false, false, false, ["Gender", "Пол"],
            ["Female", "Male"]),
        Col(Ids.MaritalStatus, "Medeni Durum", false, false, false, ["Marital Status", "Семейное положение"],
            ["Single", "Married", "Divorced", "Widowed"]),
        Col(Ids.Nationality, "Uyruk", false, false, false, ["Nationality", "Гражданство"]),
        Col(Ids.BloodType, "Kan Grubu", false, false, false, ["Blood Type", "Группа крови"],
            ["APositive", "ANegative", "BPositive", "BNegative", "AbPositive", "AbNegative", "OPositive", "ONegative"]),
        Col(Ids.EducationLevel, "Öğrenim Durumu", false, false, false, ["Education Level", "Образование"],
            ["Primary", "Secondary", "HighSchool", "Associate", "Bachelor", "Master", "Doctorate"]),
        Col(Ids.SchoolName, "Okul / Üniversite", false, false, true, ["School", "University", "Üniversite"]),
        Col(Ids.EducationDescription, "Bölüm", false, false, true, ["Field of Study", "Education Description"]),
        Col(Ids.GraduationDate, "Mezuniyet Tarihi", false, false, false, ["Graduation Date", "Дата выпуска"]),
        Col(Ids.ForeignLanguage, "Yabancı Dil", false, false, false, ["Foreign Language", "Иностранный язык"],
            ["English", "German", "French", "Arabic", "Russian", "Spanish", "Chinese", "Japanese", "Korean", "Other"]),
        Col(Ids.KepAddress, "KEP Adresi", false, false, false, ["KEP", "Registered Email"]),
        Col(Ids.DrivingLicenceCategory, "Ehliyet", false, false, false, ["Driving Licence", "Водительские права"],
            ["A", "A1", "A2", "B", "B1", "Be", "C", "Ce", "D", "De", "F", "G"]),
        Col(Ids.MilitaryServiceStatus, "Askerlik Durumu", false, false, false, ["Military Status", "Воинская обязанность"],
            ["Completed", "Exempt", "Deferred", "NotCompleted"]),
        Col(Ids.MilitaryExemptionReason, "Askerlik Muaf Nedeni", false, false, true, ["Military Exemption Reason"]),
        Col(Ids.MilitaryDefermentReason, "Askerlik Tecil Nedeni", false, false, true, ["Military Deferment Reason"]),
        Col(Ids.EmergencyName, "Acil Durum Kişisi", false, true, false, ["Emergency Contact", "Emergency Name"]),
        Col(Ids.EmergencyRelationship, "Yakınlık", false, true, false, ["Emergency Relationship", "Relationship"]),
        Col(Ids.EmergencyPhone, "Acil Durum Telefonu", false, true, false, ["Emergency Phone"]),
        Col(Ids.PaymentIban, "IBAN", false, true, false, ["Payment IBAN"]),
        Col(Ids.PaymentBankName, "Banka Adı", false, true, false, ["Bank Name", "Banka"]),
        Col(Ids.HrNotes, "Not", false, false, true, ["Notes", "Note"]),
    ];

    public static IReadOnlyDictionary<string, PersonnelImportColumn> ById { get; } =
        Columns.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, string[]> HeaderAliases { get; } =
        Columns.ToDictionary(
            item => item.Id,
            item => item.Aliases
                .Prepend(item.Header)
                .Append(DisplayHeader(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            StringComparer.OrdinalIgnoreCase);

    public static string DisplayHeader(PersonnelImportColumn column) =>
        column.Required ? column.Header + " *" : column.Header;

    public static string NormalizeHeader(string header)
    {
        var trimmed = header.Trim();
        return trimmed.EndsWith(" *", StringComparison.Ordinal)
            ? trimmed[..^2].Trim()
            : trimmed;
    }

    public static bool TryMatchHeader(string header, out string columnId)
    {
        var normalized = NormalizeHeader(header);
        foreach (var (id, aliases) in HeaderAliases)
        {
            if (aliases.Any(alias => string.Equals(NormalizeHeader(alias), normalized, StringComparison.OrdinalIgnoreCase)))
            {
                columnId = id;
                return true;
            }
        }

        columnId = string.Empty;
        return false;
    }

    public static class Ids
    {
        public const string PersonnelNumber = "personnelNumber";
        public const string GivenName = "givenName";
        public const string FamilyName = "familyName";
        public const string Department = "department";
        public const string DepartmentCode = "departmentCode";
        public const string Position = "position";
        public const string PositionCode = "positionCode";
        public const string EmploymentStartDate = "employmentStartDate";
        public const string MobilePhone = "mobilePhone";
        public const string HomePhone = "homePhone";
        public const string Email = "email";
        public const string ResidenceCity = "residenceCity";
        public const string ResidenceDistrict = "residenceDistrict";
        public const string ResidenceAddress = "residenceAddress";
        public const string NotificationAddress = "notificationAddress";
        public const string NationalIdentityScheme = "nationalIdentityScheme";
        public const string NationalIdentityNumber = "nationalIdentityNumber";
        public const string BirthDate = "birthDate";
        public const string BirthPlace = "birthPlace";
        public const string Gender = "gender";
        public const string MaritalStatus = "maritalStatus";
        public const string Nationality = "nationality";
        public const string BloodType = "bloodType";
        public const string EducationLevel = "educationLevel";
        public const string SchoolName = "schoolName";
        public const string EducationDescription = "educationDescription";
        public const string GraduationDate = "graduationDate";
        public const string ForeignLanguage = "foreignLanguage";
        public const string KepAddress = "kepAddress";
        public const string DrivingLicenceCategory = "drivingLicenceCategory";
        public const string MilitaryServiceStatus = "militaryServiceStatus";
        public const string MilitaryExemptionReason = "militaryExemptionReason";
        public const string MilitaryDefermentReason = "militaryDefermentReason";
        public const string EmergencyName = "emergencyName";
        public const string EmergencyRelationship = "emergencyRelationship";
        public const string EmergencyPhone = "emergencyPhone";
        public const string PaymentIban = "paymentIban";
        public const string PaymentBankName = "paymentBankName";
        public const string HrNotes = "hrNotes";
    }

    private static PersonnelImportColumn Col(
        string id,
        string header,
        bool required,
        bool sensitive,
        bool wrapText,
        string[] aliases,
        string[]? listValues = null) =>
        new(id, header, required, sensitive, wrapText, aliases, listValues);
}
