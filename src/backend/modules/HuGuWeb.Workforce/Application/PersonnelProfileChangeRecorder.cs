using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public static class SensitiveValueMasker
{
    public static string? MaskForHistory(string fieldCode, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return fieldCode switch
        {
            PersonnelProfileFieldCodes.NationalIdentityNumber => MaskTail(value, 4),
            PersonnelProfileFieldCodes.PaymentIban => MaskTail(value, 4),
            _ => value
        };
    }

    private static string MaskTail(string value, int visibleTail)
    {
        if (value.Length <= visibleTail)
        {
            return new string('*', value.Length);
        }

        return new string('*', value.Length - visibleTail) + value[^visibleTail..];
    }
}

public static class PersonnelProfileChangeRecorder
{
    public sealed record ProfileSnapshot(
        string GivenName,
        string FamilyName,
        string? MobilePhone,
        string? HomePhone,
        string? Email,
        string? Nationality,
        string? NationalIdentityNumber,
        NationalIdentityScheme? NationalIdentityScheme,
        string? ResidenceAddress,
        string? ResidenceCity,
        string? ResidenceDistrict,
        string? NotificationAddress,
        EducationLevel? EducationLevel,
        string? EducationDescription,
        string? SchoolName,
        BloodType? BloodType,
        string? Iban,
        string? BankName);

    public static ProfileSnapshot Capture(Employee employee, EmployeeHrProfile? profile, EmployeePaymentProfile? payment = null) =>
        new(
            employee.GivenName,
            employee.FamilyName,
            profile?.MobilePhone,
            profile?.HomePhone,
            profile?.Email,
            profile?.Nationality,
            profile?.NationalIdentityNumber,
            profile?.NationalIdentityScheme,
            profile?.ResidenceAddress,
            profile?.ResidenceCity,
            profile?.ResidenceDistrict,
            profile?.NotificationAddress,
            profile?.EducationLevel,
            profile?.EducationDescription,
            profile?.SchoolName,
            profile?.BloodType,
            payment?.Iban,
            payment?.BankName);

    public static IReadOnlyList<(string FieldCode, string? OldValue, string? NewValue)> Diff(
        ProfileSnapshot before,
        ProfileSnapshot after)
    {
        var changes = new List<(string, string?, string?)>();
        AddChange(changes, PersonnelProfileFieldCodes.GivenName, before.GivenName, after.GivenName);
        AddChange(changes, PersonnelProfileFieldCodes.FamilyName, before.FamilyName, after.FamilyName);
        AddChange(changes, PersonnelProfileFieldCodes.MobilePhone, before.MobilePhone, after.MobilePhone);
        AddChange(changes, PersonnelProfileFieldCodes.HomePhone, before.HomePhone, after.HomePhone);
        AddChange(changes, PersonnelProfileFieldCodes.Email, before.Email, after.Email);
        AddChange(changes, PersonnelProfileFieldCodes.Nationality, before.Nationality, after.Nationality);
        AddChange(changes, PersonnelProfileFieldCodes.NationalIdentityNumber, before.NationalIdentityNumber, after.NationalIdentityNumber);
        AddChange(changes, PersonnelProfileFieldCodes.NationalIdentityScheme, before.NationalIdentityScheme?.ToString(), after.NationalIdentityScheme?.ToString());
        AddChange(changes, PersonnelProfileFieldCodes.ResidenceAddress, before.ResidenceAddress, after.ResidenceAddress);
        AddChange(changes, PersonnelProfileFieldCodes.ResidenceCity, before.ResidenceCity, after.ResidenceCity);
        AddChange(changes, PersonnelProfileFieldCodes.ResidenceDistrict, before.ResidenceDistrict, after.ResidenceDistrict);
        AddChange(changes, PersonnelProfileFieldCodes.NotificationAddress, before.NotificationAddress, after.NotificationAddress);
        AddChange(changes, PersonnelProfileFieldCodes.EducationLevel, before.EducationLevel?.ToString(), after.EducationLevel?.ToString());
        AddChange(changes, PersonnelProfileFieldCodes.EducationDescription, before.EducationDescription, after.EducationDescription);
        AddChange(changes, PersonnelProfileFieldCodes.SchoolName, before.SchoolName, after.SchoolName);
        AddChange(changes, PersonnelProfileFieldCodes.BloodType, before.BloodType?.ToString(), after.BloodType?.ToString());
        AddChange(changes, PersonnelProfileFieldCodes.PaymentIban, before.Iban, after.Iban);
        AddChange(changes, PersonnelProfileFieldCodes.PaymentBankName, before.BankName, after.BankName);
        return changes;
    }

    public static void RecordDiff(
        IWorkforceStore store,
        Guid employeeId,
        Guid organizationId,
        Guid? propertyId,
        PersonnelChangeContext actor,
        IReadOnlyList<(string FieldCode, string? OldValue, string? NewValue)> changes)
    {
        foreach (var (fieldCode, oldValue, newValue) in changes)
        {
            if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
            {
                continue;
            }

            store.AddPersonnelProfileChange(PersonnelProfileChange.Record(
                Guid.CreateVersion7(),
                employeeId,
                organizationId,
                propertyId,
                fieldCode,
                SensitiveValueMasker.MaskForHistory(fieldCode, oldValue),
                SensitiveValueMasker.MaskForHistory(fieldCode, newValue),
                actor.OccurredAtUtc,
                actor.UserId,
                actor.EmployeeId,
                actor.ChangeSource));
        }
    }

    public static IReadOnlyList<(string FieldCode, string? OldValue, string? NewValue)> DiffEmployee(
        Employee before,
        Employee after)
    {
        var changes = new List<(string, string?, string?)>();
        if (!string.Equals(before.GivenName, after.GivenName, StringComparison.Ordinal))
        {
            changes.Add((PersonnelProfileFieldCodes.GivenName, before.GivenName, after.GivenName));
        }

        if (!string.Equals(before.FamilyName, after.FamilyName, StringComparison.Ordinal))
        {
            changes.Add((PersonnelProfileFieldCodes.FamilyName, before.FamilyName, after.FamilyName));
        }

        return changes;
    }

    public static IReadOnlyList<(string FieldCode, string? OldValue, string? NewValue)> DiffProfile(
        EmployeeHrProfile? before,
        EmployeeHrProfile after)
    {
        var changes = new List<(string, string?, string?)>();
        AddChange(changes, PersonnelProfileFieldCodes.NationalIdentityScheme,
            before?.NationalIdentityScheme?.ToString(), after.NationalIdentityScheme?.ToString());
        AddChange(changes, PersonnelProfileFieldCodes.NationalIdentityNumber,
            before?.NationalIdentityNumber, after.NationalIdentityNumber);
        AddChange(changes, PersonnelProfileFieldCodes.MobilePhone, before?.MobilePhone, after.MobilePhone);
        AddChange(changes, PersonnelProfileFieldCodes.HomePhone, before?.HomePhone, after.HomePhone);
        AddChange(changes, PersonnelProfileFieldCodes.Email, before?.Email, after.Email);
        AddChange(changes, PersonnelProfileFieldCodes.ResidenceAddress, before?.ResidenceAddress, after.ResidenceAddress);
        AddChange(changes, PersonnelProfileFieldCodes.ResidenceCity, before?.ResidenceCity, after.ResidenceCity);
        AddChange(changes, PersonnelProfileFieldCodes.ResidenceDistrict, before?.ResidenceDistrict, after.ResidenceDistrict);
        AddChange(changes, PersonnelProfileFieldCodes.NotificationAddress, before?.NotificationAddress, after.NotificationAddress);
        AddChange(changes, PersonnelProfileFieldCodes.Nationality, before?.Nationality, after.Nationality);
        AddChange(changes, PersonnelProfileFieldCodes.EducationLevel,
            before?.EducationLevel?.ToString(), after.EducationLevel?.ToString());
        AddChange(changes, PersonnelProfileFieldCodes.EducationDescription, before?.EducationDescription, after.EducationDescription);
        AddChange(changes, PersonnelProfileFieldCodes.SchoolName, before?.SchoolName, after.SchoolName);
        AddChange(changes, PersonnelProfileFieldCodes.BloodType,
            before?.BloodType?.ToString(), after.BloodType?.ToString());
        return changes;
    }

    public static IReadOnlyList<(string FieldCode, string? OldValue, string? NewValue)> DiffPaymentProfile(
        EmployeePaymentProfile? before,
        EmployeePaymentProfile after)
    {
        var changes = new List<(string, string?, string?)>();
        AddChange(changes, PersonnelProfileFieldCodes.PaymentIban, before?.Iban, after.Iban);
        AddChange(changes, PersonnelProfileFieldCodes.PaymentBankName, before?.BankName, after.BankName);
        return changes;
    }

    public static IReadOnlyList<(string FieldCode, string? OldValue, string? NewValue)> DiffEmergencyContacts(
        IReadOnlyList<EmergencyContact> before,
        IReadOnlyList<EmergencyContact> after)
    {
        static string Format(IReadOnlyList<EmergencyContact> contacts) =>
            string.Join("; ", contacts
                .OrderBy(item => item.SortOrder)
                .Select(item => $"{item.Name}|{item.Phone}"));

        var oldFormatted = Format(before);
        var newFormatted = Format(after);
        if (string.Equals(oldFormatted, newFormatted, StringComparison.Ordinal))
        {
            return [];
        }

        return [(PersonnelProfileFieldCodes.EmergencyContacts, oldFormatted, newFormatted)];
    }

    private static void AddChange(
        List<(string FieldCode, string? OldValue, string? NewValue)> changes,
        string fieldCode,
        string? oldValue,
        string? newValue)
    {
        if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            changes.Add((fieldCode, oldValue, newValue));
        }
    }
}
