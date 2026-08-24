namespace HuGuWeb.Workforce.Domain;

public static class Iso3166Alpha2Catalog
{
    public const int CodeLength = 2;

    public static IReadOnlyList<string> Codes { get; } = Parse(AssignedCodes);

    public static IReadOnlySet<string> CodeSet { get; } = Codes.ToHashSet(StringComparer.Ordinal);

    public static bool TryNormalize(string? value, out string? code, out string? error)
    {
        code = null;
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim().ToUpperInvariant();
        if (trimmed.Length != CodeLength || !CodeSet.Contains(trimmed))
        {
            error = HrValidation.Codes.InvalidNationality;
            return false;
        }

        code = trimmed;
        return true;
    }

    public static bool Contains(string? code) =>
        !string.IsNullOrEmpty(code) && CodeSet.Contains(code);

    private static IReadOnlyList<string> Parse(string packed) =>
        packed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // Officially assigned ISO 3166-1 alpha-2 codes (249). User-assigned (e.g. XK) are excluded.
    private const string AssignedCodes =
        "AD AE AF AG AI AL AM AO AQ AR AS AT AU AW AX AZ " +
        "BA BB BD BE BF BG BH BI BJ BL BM BN BO BQ BR BS BT BV BW BY BZ " +
        "CA CC CD CF CG CH CI CK CL CM CN CO CR CU CV CW CX CY CZ " +
        "DE DJ DK DM DO DZ " +
        "EC EE EG EH ER ES ET " +
        "FI FJ FK FM FO FR " +
        "GA GB GD GE GF GG GH GI GL GM GN GP GQ GR GS GT GU GW GY " +
        "HK HM HN HR HT HU " +
        "ID IE IL IM IN IO IQ IR IS IT " +
        "JE JM JO JP " +
        "KE KG KH KI KM KN KP KR KW KY KZ " +
        "LA LB LC LI LK LR LS LT LU LV LY " +
        "MA MC MD ME MF MG MH MK ML MM MN MO MP MQ MR MS MT MU MV MW MX MY MZ " +
        "NA NC NE NF NG NI NL NO NP NR NU NZ " +
        "OM " +
        "PA PE PF PG PH PK PL PM PN PR PS PT PW PY " +
        "QA " +
        "RE RO RS RU RW " +
        "SA SB SC SD SE SG SH SI SJ SK SL SM SN SO SR SS ST SV SX SY SZ " +
        "TC TD TF TG TH TJ TK TL TM TN TO TR TT TV TW TZ " +
        "UA UG UM US UY UZ " +
        "VA VC VE VG VI VN VU " +
        "WF WS " +
        "YE YT " +
        "ZA ZM ZW";
}
