using System.Globalization;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public static class PersonnelImportValueParser
{
    public static bool TryParseDate(string? raw, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        return DateOnly.TryParse(raw, new CultureInfo("tr-TR"), DateTimeStyles.None, out date);
    }

    public static bool TryParseOptionalDate(string? raw, out DateOnly? date, out bool invalid)
    {
        date = null;
        invalid = false;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (!TryParseDate(raw, out var parsed))
        {
            invalid = true;
            return false;
        }

        date = parsed;
        return true;
    }

    public static bool TryParseEnum<TEnum>(string? raw, out TEnum? value, out bool invalid)
        where TEnum : struct, Enum
    {
        value = null;
        invalid = false;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        var key = NormalizeKey(raw);
        if (Aliases.TryGetValue(typeof(TEnum), out var map) && map.TryGetValue(key, out var aliased)
            && aliased is TEnum parsedAlias)
        {
            value = parsedAlias;
            return true;
        }

        if (Enum.TryParse<TEnum>(raw.Trim(), ignoreCase: true, out var parsed))
        {
            value = parsed;
            return true;
        }

        invalid = true;
        return false;
    }

    private static string NormalizeKey(string raw) =>
        raw.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    private static readonly Dictionary<Type, Dictionary<string, object>> Aliases = new()
    {
        [typeof(NationalIdentityScheme)] = Map(
            ("TCKN", NationalIdentityScheme.Tckn),
            ("TC", NationalIdentityScheme.Tckn),
            ("TCKIMLIK", NationalIdentityScheme.Tckn),
            ("YKN", NationalIdentityScheme.Ykn),
            ("PASSPORT", NationalIdentityScheme.Passport),
            ("PASAPORT", NationalIdentityScheme.Passport),
            ("OTHER", NationalIdentityScheme.Other),
            ("DIGER", NationalIdentityScheme.Other),
            ("DİĞER", NationalIdentityScheme.Other)),
        [typeof(Gender)] = Map(
            ("FEMALE", Gender.Female),
            ("KADIN", Gender.Female),
            ("K", Gender.Female),
            ("F", Gender.Female),
            ("MALE", Gender.Male),
            ("ERKEK", Gender.Male),
            ("E", Gender.Male),
            ("M", Gender.Male)),
        [typeof(MaritalStatus)] = Map(
            ("SINGLE", MaritalStatus.Single),
            ("BEKAR", MaritalStatus.Single),
            ("MARRIED", MaritalStatus.Married),
            ("EVLI", MaritalStatus.Married),
            ("EVLİ", MaritalStatus.Married),
            ("DIVORCED", MaritalStatus.Divorced),
            ("BOSANMIS", MaritalStatus.Divorced),
            ("BOŞANMIŞ", MaritalStatus.Divorced),
            ("WIDOWED", MaritalStatus.Widowed),
            ("DUL", MaritalStatus.Widowed)),
        [typeof(BloodType)] = Map(
            ("A+", BloodType.APositive),
            ("APOSITIVE", BloodType.APositive),
            ("A-", BloodType.ANegative),
            ("ANEGATIVE", BloodType.ANegative),
            ("B+", BloodType.BPositive),
            ("BPOSITIVE", BloodType.BPositive),
            ("B-", BloodType.BNegative),
            ("BNEGATIVE", BloodType.BNegative),
            ("AB+", BloodType.AbPositive),
            ("ABPOSITIVE", BloodType.AbPositive),
            ("AB-", BloodType.AbNegative),
            ("ABNEGATIVE", BloodType.AbNegative),
            ("O+", BloodType.OPositive),
            ("0+", BloodType.OPositive),
            ("OPOSITIVE", BloodType.OPositive),
            ("O-", BloodType.ONegative),
            ("0-", BloodType.ONegative),
            ("ONEGATIVE", BloodType.ONegative)),
        [typeof(EducationLevel)] = Map(
            ("PRIMARY", EducationLevel.Primary),
            ("ILKOKUL", EducationLevel.Primary),
            ("İLKOKUL", EducationLevel.Primary),
            ("SECONDARY", EducationLevel.Secondary),
            ("ORTAOKUL", EducationLevel.Secondary),
            ("HIGHSCHOOL", EducationLevel.HighSchool),
            ("LISE", EducationLevel.HighSchool),
            ("LİSE", EducationLevel.HighSchool),
            ("ASSOCIATE", EducationLevel.Associate),
            ("ONLISANS", EducationLevel.Associate),
            ("ÖNLİSANS", EducationLevel.Associate),
            ("BACHELOR", EducationLevel.Bachelor),
            ("LISANS", EducationLevel.Bachelor),
            ("LİSANS", EducationLevel.Bachelor),
            ("MASTER", EducationLevel.Master),
            ("YUKSEKLISANS", EducationLevel.Master),
            ("YÜKSEKLİSANS", EducationLevel.Master),
            ("DOCTORATE", EducationLevel.Doctorate),
            ("DOKTORA", EducationLevel.Doctorate)),
        [typeof(MilitaryServiceStatus)] = Map(
            ("COMPLETED", MilitaryServiceStatus.Completed),
            ("YAPTI", MilitaryServiceStatus.Completed),
            ("YAPILDI", MilitaryServiceStatus.Completed),
            ("EXEMPT", MilitaryServiceStatus.Exempt),
            ("MUAF", MilitaryServiceStatus.Exempt),
            ("DEFERRED", MilitaryServiceStatus.Deferred),
            ("TECILLI", MilitaryServiceStatus.Deferred),
            ("TECİLLİ", MilitaryServiceStatus.Deferred),
            ("NOTCOMPLETED", MilitaryServiceStatus.NotCompleted),
            ("YAPMADI", MilitaryServiceStatus.NotCompleted)),
        [typeof(DrivingLicenceCategory)] = Map(
            ("BE", DrivingLicenceCategory.Be),
            ("CE", DrivingLicenceCategory.Ce),
            ("DE", DrivingLicenceCategory.De)),
        [typeof(ForeignLanguageSummary)] = Map(
            ("ENGLISH", ForeignLanguageSummary.English),
            ("INGILIZCE", ForeignLanguageSummary.English),
            ("İNGİLİZCE", ForeignLanguageSummary.English),
            ("GERMAN", ForeignLanguageSummary.German),
            ("ALMANCA", ForeignLanguageSummary.German),
            ("FRENCH", ForeignLanguageSummary.French),
            ("FRANSIZCA", ForeignLanguageSummary.French),
            ("ARABIC", ForeignLanguageSummary.Arabic),
            ("ARAPCA", ForeignLanguageSummary.Arabic),
            ("ARAPÇA", ForeignLanguageSummary.Arabic),
            ("RUSSIAN", ForeignLanguageSummary.Russian),
            ("RUSCA", ForeignLanguageSummary.Russian),
            ("RUSÇA", ForeignLanguageSummary.Russian),
            ("SPANISH", ForeignLanguageSummary.Spanish),
            ("ISPANYOLCA", ForeignLanguageSummary.Spanish),
            ("İSPANYOLCA", ForeignLanguageSummary.Spanish),
            ("CHINESE", ForeignLanguageSummary.Chinese),
            ("CİNCE", ForeignLanguageSummary.Chinese),
            ("ÇİNCE", ForeignLanguageSummary.Chinese),
            ("JAPANESE", ForeignLanguageSummary.Japanese),
            ("JAPONCA", ForeignLanguageSummary.Japanese),
            ("KOREAN", ForeignLanguageSummary.Korean),
            ("KORECE", ForeignLanguageSummary.Korean),
            ("OTHER", ForeignLanguageSummary.Other),
            ("DIGER", ForeignLanguageSummary.Other),
            ("DİĞER", ForeignLanguageSummary.Other)),
    };

    private static Dictionary<string, object> Map(params (string Key, object Value)[] pairs)
    {
        var map = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (key, value) in pairs)
        {
            map[NormalizeKey(key)] = value;
        }

        return map;
    }
}
