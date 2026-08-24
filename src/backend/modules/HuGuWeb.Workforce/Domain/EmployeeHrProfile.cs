namespace HuGuWeb.Workforce.Domain;

public sealed class EmployeeHrProfile
{
    private EmployeeHrProfile()
    {
    }

    private EmployeeHrProfile(Guid id, Guid employeeId, Guid organizationId)
    {
        Id = id;
        EmployeeId = employeeId;
        OrganizationId = organizationId;
    }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public Guid OrganizationId { get; private set; }
    public NationalIdentityScheme? NationalIdentityScheme { get; private set; }
    public string? NationalIdentityNumber { get; private set; }
    public string? NormalizedNationalIdentityNumber { get; private set; }
    public string? Nationality { get; private set; }
    public Gender? Gender { get; private set; }
    public DateOnly? BirthDate { get; private set; }
    public string? BirthPlace { get; private set; }
    public MaritalStatus? MaritalStatus { get; private set; }
    public BloodType? BloodType { get; private set; }
    public EducationLevel? EducationLevel { get; private set; }
    public string? MobilePhone { get; private set; }
    public string? HomePhone { get; private set; }
    public string? Email { get; private set; }
    public string? ResidenceAddress { get; private set; }
    public string? ResidenceCity { get; private set; }
    public string? ResidenceDistrict { get; private set; }
    public string? NotificationAddress { get; private set; }
    public string? HrNotes { get; private set; }

    public static EmployeeHrProfile Create(Guid id, Guid employeeId, Guid organizationId) =>
        new(id, employeeId, organizationId);

    public bool TryApply(EmployeeHrProfileValues values, DateOnly today, out string? field, out string? code)
    {
        field = null;
        code = null;
        if (!NationalIdentity.TryNormalize(
                values.NationalIdentityScheme,
                values.NationalIdentityNumber,
                out var scheme,
                out var displayNumber,
                out var normalizedNumber,
                out code))
        {
            field = code == HrValidation.Codes.IdentitySchemeRequired
                ? HrValidation.Fields.NationalIdentityScheme
                : HrValidation.Fields.NationalIdentityNumber;
            return false;
        }

        if (!ContactValue.TryNormalizeOptionalText(
                values.Nationality,
                ContactValue.NationalityMaxLength,
                out var nationality,
                out code))
        {
            field = HrValidation.Fields.Nationality;
            return false;
        }

        if (!ContactValue.TryNormalizeBirthDate(values.BirthDate, today, out var birthDate, out code))
        {
            field = HrValidation.Fields.BirthDate;
            return false;
        }

        if (!ContactValue.TryNormalizeOptionalText(
                values.BirthPlace,
                ContactValue.PlaceMaxLength,
                out var birthPlace,
                out code))
        {
            field = HrValidation.Fields.BirthPlace;
            return false;
        }

        if (!ContactValue.TryNormalizePhone(values.MobilePhone, required: false, out var mobile, out code))
        {
            field = HrValidation.Fields.MobilePhone;
            return false;
        }

        if (!ContactValue.TryNormalizePhone(values.HomePhone, required: false, out var homePhone, out code))
        {
            field = HrValidation.Fields.HomePhone;
            return false;
        }

        if (!ContactValue.TryNormalizeEmail(values.Email, out var email, out code))
        {
            field = HrValidation.Fields.Email;
            return false;
        }

        if (!ContactValue.TryNormalizeOptionalText(
                values.ResidenceAddress,
                ContactValue.AddressMaxLength,
                out var residenceAddress,
                out code))
        {
            field = HrValidation.Fields.ResidenceAddress;
            return false;
        }

        if (!ContactValue.TryNormalizeOptionalText(
                values.ResidenceCity,
                ContactValue.PlaceMaxLength,
                out var city,
                out code))
        {
            field = HrValidation.Fields.ResidenceCity;
            return false;
        }

        if (!ContactValue.TryNormalizeOptionalText(
                values.ResidenceDistrict,
                ContactValue.PlaceMaxLength,
                out var district,
                out code))
        {
            field = HrValidation.Fields.ResidenceDistrict;
            return false;
        }

        if (!ContactValue.TryNormalizeOptionalText(
                values.NotificationAddress,
                ContactValue.AddressMaxLength,
                out var notificationAddress,
                out code))
        {
            field = HrValidation.Fields.NotificationAddress;
            return false;
        }

        if (!ContactValue.TryNormalizeOptionalText(
                values.HrNotes,
                ContactValue.NotesMaxLength,
                out var notes,
                out code))
        {
            field = HrValidation.Fields.HrNotes;
            return false;
        }

        NationalIdentityScheme = scheme;
        NationalIdentityNumber = displayNumber;
        NormalizedNationalIdentityNumber = normalizedNumber;
        Nationality = nationality;
        Gender = values.Gender;
        BirthDate = birthDate;
        BirthPlace = birthPlace;
        MaritalStatus = values.MaritalStatus;
        BloodType = values.BloodType;
        EducationLevel = values.EducationLevel;
        MobilePhone = mobile;
        HomePhone = homePhone;
        Email = email;
        ResidenceAddress = residenceAddress;
        ResidenceCity = city;
        ResidenceDistrict = district;
        NotificationAddress = notificationAddress;
        HrNotes = notes;
        field = null;
        code = null;
        return true;
    }

    public bool HasNationalIdentity => NormalizedNationalIdentityNumber is not null && NationalIdentityScheme is not null;
}

public sealed record EmployeeHrProfileValues(
    NationalIdentityScheme? NationalIdentityScheme,
    string? NationalIdentityNumber,
    string? Nationality,
    Gender? Gender,
    DateOnly? BirthDate,
    string? BirthPlace,
    MaritalStatus? MaritalStatus,
    BloodType? BloodType,
    EducationLevel? EducationLevel,
    string? MobilePhone,
    string? HomePhone,
    string? Email,
    string? ResidenceAddress,
    string? ResidenceCity,
    string? ResidenceDistrict,
    string? NotificationAddress,
    string? HrNotes);
