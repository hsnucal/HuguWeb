using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Infrastructure.Seeding;

/// <summary>
/// DEVELOPMENT-ONLY workplace blueprints keyed by stable department/position codes.
/// Applicability and assignments must never depend on Ankara PropertyId alone.
/// </summary>
public static class DevelopmentWorkplaceCatalog
{
    public static readonly Guid AntalyaHumanResourcesDepartmentId =
        Guid.Parse("a1e1c0de-0001-4000-8000-000000000201");
    public static readonly Guid AntalyaHousekeepingDepartmentId =
        Guid.Parse("a1e1c0de-0001-4000-8000-000000000202");
    public static readonly Guid AntalyaFrontOfficeDepartmentId =
        Guid.Parse("a1e1c0de-0001-4000-8000-000000000203");
    public static readonly Guid AntalyaTechnicalDepartmentId =
        Guid.Parse("a1e1c0de-0001-4000-8000-000000000204");
    public static readonly Guid AntalyaFoodBeverageDepartmentId =
        Guid.Parse("a1e1c0de-0001-4000-8000-000000000205");

    public static (int OrganizationalLevel, bool CanManageEmployees) HierarchyForCode(string code) =>
        code switch
        {
            "HK-SUP" => (200, true),
            "FO-MGR" => (300, true),
            _ => (100, false)
        };

    public static readonly IReadOnlyList<(string Name, string Code)> Positions =
    [
        ("Kat Görevlisi", "HK-ATT"),
        ("Kat Hizmetleri Sorumlusu", "HK-SUP"),
        ("Minibar Görevlisi", "HK-MIN"),
        ("Resepsiyon Görevlisi", "FO-REC"),
        ("Ön Büro Müdürü", "FO-MGR"),
        ("Garson", "FNB-WAIT"),
        ("Aşçı", "FNB-CHEF"),
        ("Teknisyen", "ENG-TECH"),
        ("İK Uzmanı", "HR-OFF"),
        ("Uzman", "SPEC")
    ];

    public static readonly IReadOnlyList<(string PositionCode, string[] DepartmentCodes)> ApplicabilityMaps =
    [
        ("HK-ATT", ["HK"]),
        ("HK-SUP", ["HK"]),
        ("HK-MIN", ["HK"]),
        ("FO-REC", ["FO"]),
        ("FO-MGR", ["FO"]),
        ("FNB-WAIT", ["FNB"]),
        ("FNB-CHEF", ["FNB"]),
        ("ENG-TECH", ["ENG"]),
        ("HR-OFF", ["HR"]),
        ("SPEC", ["HR", "ENG"])
    ];

    public static IReadOnlyList<(Guid Id, string Name, string Code)> DepartmentsFor(Guid propertyId)
    {
        if (propertyId == DevelopmentWorkforceSeeder.AnkaraPropertyId)
        {
            return
            [
                (DevelopmentWorkforceSeeder.HumanResourcesDepartmentId, "İnsan Kaynakları", "HR"),
                (DevelopmentWorkforceSeeder.HousekeepingDepartmentId, "Kat Hizmetleri", "HK"),
                (DevelopmentWorkforceSeeder.FrontOfficeDepartmentId, "Ön Büro", "FO"),
                (DevelopmentWorkforceSeeder.TechnicalDepartmentId, "Teknik Servis", "ENG"),
                (DevelopmentWorkforceSeeder.FoodBeverageDepartmentId, "Yiyecek ve İçecek", "FNB")
            ];
        }

        if (propertyId == DevelopmentWorkforceSeeder.AntalyaPropertyId)
        {
            return
            [
                (AntalyaHumanResourcesDepartmentId, "İnsan Kaynakları", "HR"),
                (AntalyaHousekeepingDepartmentId, "Kat Hizmetleri", "HK"),
                (AntalyaFrontOfficeDepartmentId, "Ön Büro", "FO"),
                (AntalyaTechnicalDepartmentId, "Teknik Servis", "ENG"),
                (AntalyaFoodBeverageDepartmentId, "Yiyecek ve İçecek", "FNB")
            ];
        }

        return [];
    }

    public static IReadOnlyList<(Guid DepartmentId, Guid PositionId)> ResolveApplicability(
        IReadOnlyList<Department> departments,
        IReadOnlyList<Position> positions)
    {
        var results = new List<(Guid DepartmentId, Guid PositionId)>();
        var propertyIds = departments.Select(item => item.PropertyId)
            .Concat(positions.Select(item => item.PropertyId))
            .Distinct();

        foreach (var propertyId in propertyIds)
        {
            var departmentByCode = departments
                .Where(item => item.PropertyId == propertyId && item.Code is not null)
                .GroupBy(item => item.Code!, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.Ordinal);
            var positionByCode = positions
                .Where(item => item.PropertyId == propertyId && item.Code is not null)
                .GroupBy(item => item.Code!, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.Ordinal);

            foreach (var (positionCode, departmentCodes) in ApplicabilityMaps)
            {
                if (!positionByCode.TryGetValue(positionCode, out var positionId))
                {
                    continue;
                }

                foreach (var departmentCode in departmentCodes)
                {
                    if (!departmentByCode.TryGetValue(departmentCode, out var departmentId))
                    {
                        continue;
                    }

                    results.Add((departmentId, positionId));
                }
            }
        }

        return results;
    }
}
