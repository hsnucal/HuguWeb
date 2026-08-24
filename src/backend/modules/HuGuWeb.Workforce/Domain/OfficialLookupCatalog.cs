namespace HuGuWeb.Workforce.Domain;

public static class OfficialLookupCatalog
{
    public const string OccupationCatalogueSource = "webik-reference-snapshot";
    public const string OccupationCatalogueVersion = "webik-2026-08-24";

    public static IReadOnlyList<(string Code, string Description)> DocumentTypes { get; } =
    [
        ("01", "AYLIK SİGORTA PRİM BİLDİRGESİ"),
        ("02", "SOSYAL GÜVENLİK DESTEK PRİM BİLDİRGESİ"),
        ("03", "DENİZ, BASIM, AZOT, ŞEKER"),
        ("04", "YERALTI SÜREKLİ"),
        ("05", "YERALTI GRUPLU"),
        ("06", "YERÜSTÜ GRUPLU"),
        ("07", "ÇIRAK/STAJYER ÖĞRENCİ"),
        ("11", "Y.Ö.K.KISMİ İTİH. ÖĞRENCİ"),
        ("12", "GEÇİCİ 20. MADDEYE TABİ OLANLAR"),
        ("13", "AYLIK SİGORTA PRİM İŞSİZLİK HARİÇ"),
        ("19", "CEZA İNFAZ KURUMLARI"),
        ("28", "STAJYER AV./İŞÇİ"),
        ("29", "İŞKUR MESLEK EDİNDİRME"),
        ("32", "TARBİL"),
        ("33", "İŞKUR TOP. İŞ PROG."),
        ("39", "YABANCI UYRUKLU"),
        ("42", "STAJYER (4/a-b)"),
        ("44", "İŞKUR GENÇLİK PROG."),
        ("46", "YURT DIŞI BORÇLANMA"),
        ("48", "İŞKUR İŞBAŞI EĞİTİM"),
        ("49", "50 VE ÜZERİ"),
        ("50", "Lise/Üni Stajyer"),
        ("51", "Harp Malülü"),
        ("55", "EV HİZMETLERİ")
    ];

    public static IReadOnlyList<(string Code, string Description)> ApplicableLaws { get; } =
    [
        ("00000", "SİGORTALI BİR KANUNA TABİ DEĞİL"),
        ("04325", "SİGORTALI OLAĞANÜSTÜ HAL KANUNUNA TABİ"),
        ("04369", "SGORTALI SENDİKA İNDİRİMİ KANUNUNA TABİ"),
        ("04382", "SİGORTALI SAKATLIK İNDİRİMİ KANUNUNA TABİ"),
        ("04447", "4447 SAYILI KANUN"),
        ("04747", "SİGORTALI BORÇ ERTELEME İNDİRİMİNE TABİ İSE"),
        ("04857", "SİGORTALI SAKATLIK, E.HÜKÜMLÜ-TERÖR İNDİRİMİ KANUNUNA TABİ İSE"),
        ("05084", "HAZİNE İNDİRİMİNE %100"),
        ("05510", "HAZİNE İNDİRİMİ"),
        ("05921", "HAZİNE İNDİRİMİ %100"),
        ("06111", "OZ IND"),
        ("06645", "İŞKUR İŞBAŞI EĞİTİM"),
        ("14857", "KONTENJAN SINIRI İÇİNDEKİ ÖZÜRLÜ İŞÇİ"),
        ("27103", "27103 SAYILI KHK"),
        ("47473", "SENDİKALI SİGORTALI BORÇ ERTELEME İNDİRİMİNE TABİ"),
        ("54857", "%100 SİGORTALI SAKATLIK, E HÜKÜMLÜ-TERÖR İNDİRİMİ KANUNUNA TABİ"),
        ("85084", "HAZİNE İNDİRİMİ %80"),
        ("46486", "46846 SAYILI KHK"),
        ("16322", "YATIRIM BELGESİ TEŞVİKİ"),
        ("07252", "4447/GEÇİCİ 26. MADDE (KÇÖ YARARLANANLAR)"),
        ("27256", "27256 SAYILI TEŞVİK"),
        ("17103", "17103 SAYILI KHK"),
        ("07256", "7256 SAYILI KHK"),
        ("03294", "3294 SAYILI KHK"),
        ("05746", "05746-04691 SAYILI KHK"),
        ("02828", "02828 SAYILI KHK"),
        ("15510", "EYT TEŞVİK İNDİRİMİ %5")
    ];

    public static IReadOnlyList<(string Code, string Description)> InsuranceBranches { get; } =
    [
        ("00", "Tüm Sigorta Kolları"),
        ("07", "Çırak"),
        ("08", "Sosyal Güvenlik Destek Primi"),
        ("12", "U.Söz Olmayan Yab.Uyrk.Sigortalı"),
        ("14", "Cezaevi Çalışanları"),
        ("16", "İşkur Kursiyerleri"),
        ("17", "İş Kaybı Tazminatı Alanlar"),
        ("18", "YÖK ve ÖSYM Kısmi İstihdam")
    ];

    public static IReadOnlyList<(string Code, string Description)> DutyCodes { get; } =
    [
        ("EmployerOrRepresentative", "İşveren veya Vekili"),
        ("Worker", "İşçi"),
        ("CivilServant4B", "657 SK (4/b) Kapsamında Çalışanlar"),
        ("CivilServant4C", "657 SK (4/c) Kapsamında Çalışanlar"),
        ("ApprenticeOrIntern", "Çıraklar ve Stajer Öğrenciler"),
        ("Other", "Diğerleri")
    ];
}
