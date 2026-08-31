namespace HuGuWeb.Workforce.Infrastructure.Seeding;

/// <summary>
/// Deterministic DEVELOPMENT employee fixtures for employee-like personas.
/// Runtime authorization never reads emails — only EmployeeAccountLink IDs.
/// </summary>
public static class DevelopmentPersonaEmployeeFixtures
{
    public static readonly Guid MaintenanceTechnicianEmployeeId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000401");
    public static readonly Guid MaintenanceTechnicianEmploymentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000402");
    public static readonly Guid MaintenanceTechnicianAssignmentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000403");
    public static readonly Guid MaintenanceTechnicianLinkId =
        Guid.Parse("b1e1c0de-0002-4000-8000-000000000001");

    public static readonly Guid HrManagerEmployeeId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000411");
    public static readonly Guid HrManagerEmploymentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000412");
    public static readonly Guid HrManagerAssignmentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000413");
    public static readonly Guid HrManagerLinkId =
        Guid.Parse("b1e1c0de-0002-4000-8000-000000000002");

    public static readonly Guid RoomAttendantEmployeeId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000421");
    public static readonly Guid RoomAttendantEmploymentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000422");
    public static readonly Guid RoomAttendantAssignmentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000423");
    public static readonly Guid RoomAttendantLinkId =
        Guid.Parse("b1e1c0de-0002-4000-8000-000000000003");

    public static readonly Guid MaintenanceManagerEmployeeId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000431");
    public static readonly Guid MaintenanceManagerEmploymentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000432");
    public static readonly Guid MaintenanceManagerAssignmentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000433");
    public static readonly Guid MaintenanceManagerLinkId =
        Guid.Parse("b1e1c0de-0002-4000-8000-000000000004");

    public static readonly Guid RoomInspectorEmployeeId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000441");
    public static readonly Guid RoomInspectorEmploymentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000442");
    public static readonly Guid RoomInspectorAssignmentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000443");
    public static readonly Guid RoomInspectorLinkId =
        Guid.Parse("b1e1c0de-0002-4000-8000-000000000005");

    public static readonly Guid RoomOpsManagerEmployeeId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000451");
    public static readonly Guid RoomOpsManagerEmploymentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000452");
    public static readonly Guid RoomOpsManagerAssignmentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000453");
    public static readonly Guid RoomOpsManagerLinkId =
        Guid.Parse("b1e1c0de-0002-4000-8000-000000000006");

    public static readonly Guid AntalyaHrManagerEmployeeId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000461");
    public static readonly Guid AntalyaHrManagerEmploymentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000462");
    public static readonly Guid AntalyaHrManagerAssignmentId =
        Guid.Parse("a1e1c0de-0003-4000-8000-000000000463");
    public static readonly Guid AntalyaHrManagerLinkId =
        Guid.Parse("b1e1c0de-0002-4000-8000-000000000007");

    public static readonly DateOnly EmploymentStartDate = new(2026, 1, 1);

    public static IReadOnlyList<PersonaEmployeeFixture> All { get; } =
    [
        new(
            "maintenance.technician@localhost",
            MaintenanceTechnicianEmployeeId,
            MaintenanceTechnicianEmploymentId,
            MaintenanceTechnicianAssignmentId,
            MaintenanceTechnicianLinkId,
            DevelopmentWorkforceSeeder.AnkaraPropertyId,
            "DEMO-TECH-01",
            "Ali",
            "Tekin",
            "ENG",
            "Teknik Servis",
            DevelopmentWorkforceSeeder.TechnicalDepartmentId,
            "ENG-TECH"),
        new(
            "hr.manager@localhost",
            HrManagerEmployeeId,
            HrManagerEmploymentId,
            HrManagerAssignmentId,
            HrManagerLinkId,
            DevelopmentWorkforceSeeder.AnkaraPropertyId,
            "DEMO-HR-01",
            "Ayşe",
            "Yılmaz",
            "HR",
            "İnsan Kaynakları",
            DevelopmentWorkforceSeeder.HumanResourcesDepartmentId,
            "HR-OFF"),
        new(
            "roomops.attendant@localhost",
            RoomAttendantEmployeeId,
            RoomAttendantEmploymentId,
            RoomAttendantAssignmentId,
            RoomAttendantLinkId,
            DevelopmentWorkforceSeeder.AnkaraPropertyId,
            "DEMO-HK-01",
            "Zeynep",
            "Demir",
            "HK",
            "Kat Hizmetleri",
            DevelopmentWorkforceSeeder.HousekeepingDepartmentId,
            "HK-ATT",
            AlternateDepartmentCodes: ["KH"]),
        new(
            "maintenance.manager@localhost",
            MaintenanceManagerEmployeeId,
            MaintenanceManagerEmploymentId,
            MaintenanceManagerAssignmentId,
            MaintenanceManagerLinkId,
            DevelopmentWorkforceSeeder.AnkaraPropertyId,
            "DEMO-TECH-MGR-01",
            "Murat",
            "Kaya",
            "ENG",
            "Teknik Servis",
            DevelopmentWorkforceSeeder.TechnicalDepartmentId,
            "ENG-TECH"),
        new(
            "roomops.inspector@localhost",
            RoomInspectorEmployeeId,
            RoomInspectorEmploymentId,
            RoomInspectorAssignmentId,
            RoomInspectorLinkId,
            DevelopmentWorkforceSeeder.AnkaraPropertyId,
            "DEMO-HK-INS-01",
            "Elif",
            "Şahin",
            "HK",
            "Kat Hizmetleri",
            DevelopmentWorkforceSeeder.HousekeepingDepartmentId,
            "HK-SUP",
            AlternateDepartmentCodes: ["KH"],
            AlternatePositionCodes: ["HK-KSHF", "HK-ATT"]),
        new(
            "roomops.manager@localhost",
            RoomOpsManagerEmployeeId,
            RoomOpsManagerEmploymentId,
            RoomOpsManagerAssignmentId,
            RoomOpsManagerLinkId,
            DevelopmentWorkforceSeeder.AnkaraPropertyId,
            "DEMO-HK-MGR-01",
            "Selin",
            "Arslan",
            "HK",
            "Kat Hizmetleri",
            DevelopmentWorkforceSeeder.HousekeepingDepartmentId,
            "HK-SUP",
            AlternateDepartmentCodes: ["KH"],
            AlternatePositionCodes: ["HK-KSHF"]),
        new(
            "hr.antalya@localhost",
            AntalyaHrManagerEmployeeId,
            AntalyaHrManagerEmploymentId,
            AntalyaHrManagerAssignmentId,
            AntalyaHrManagerLinkId,
            DevelopmentWorkforceSeeder.AntalyaPropertyId,
            "DEMO-HR-AYT-01",
            "Deniz",
            "Aksoy",
            "HR",
            "İnsan Kaynakları",
            Guid.Parse("a1e1c0de-0001-4000-8000-000000000201"),
            "HR-OFF")
    ];

    public static IReadOnlySet<Guid> EmployeeIds { get; } =
        All.Select(item => item.EmployeeId).ToHashSet();

    public static PersonaEmployeeFixture? ByPersonaEmail(string email) =>
        All.FirstOrDefault(item => item.PersonaEmail.Equals(email, StringComparison.OrdinalIgnoreCase));
}

public sealed record PersonaEmployeeFixture(
    string PersonaEmail,
    Guid EmployeeId,
    Guid EmploymentId,
    Guid AssignmentId,
    Guid AccountLinkId,
    Guid PropertyId,
    string PersonnelNumber,
    string GivenName,
    string FamilyName,
    string DepartmentCode,
    string DepartmentName,
    Guid PreferredDepartmentId,
    string PositionCode,
    IReadOnlyList<string>? AlternateDepartmentCodes = null,
    IReadOnlyList<string>? AlternatePositionCodes = null);
