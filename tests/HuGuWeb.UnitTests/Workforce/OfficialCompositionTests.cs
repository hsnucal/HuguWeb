using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class OfficialCompositionTests
{
    [Fact]
    public void OccupationCatalogue_ParsesFullReferenceArtifact()
    {
        var json = File.ReadAllText(CataloguePath());
        var document = SgkOccupationCatalogueParser.Parse(json);

        Assert.Equal(7765, document.Occupations.Count);
        Assert.Equal(7765, document.Occupations.Select(item => item.Code).Distinct().Count());
        Assert.Equal("webik-reference-snapshot", document.Source);
        Assert.Contains(document.Occupations, item => item.Code == "5120.10" && item.Name.Contains("Aşçı"));
        Assert.Contains(document.Occupations, item => item.Code == "0110.00" && item.Name == "Subaylar");
        Assert.All(document.Occupations, item => Assert.True(SgkOccupationCode.IsValidFormat(item.Code)));
    }

    [Fact]
    public void OccupationCatalogue_SyncFromCatalogue_IsIdempotentForSameDescription()
    {
        var code = new SgkOccupationCode("5120.10", "Aşçı", true, "old", "v0");
        Assert.True(code.SyncFromCatalogue("Aşçıbaşı", true, "webik-reference-snapshot", "webik-2026-08-24"));
        Assert.Equal("Aşçıbaşı", code.Description);
        Assert.False(code.SyncFromCatalogue("Aşçıbaşı", true, "webik-reference-snapshot", "webik-2026-08-24"));
    }

    [Fact]
    public void OccupationCatalogue_RejectsDuplicateCodes()
    {
        const string json = """
            {
              "source": "test",
              "catalogueVersion": "v1",
              "occupations": [
                { "code": "5120.10", "name": "Aşçı", "isActive": true },
                { "code": "5120.10", "name": "Aşçıbaşı", "isActive": true }
              ]
            }
            """;

        var error = Assert.Throws<InvalidOperationException>(() => SgkOccupationCatalogueParser.Parse(json));
        Assert.Contains("duplicate", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OccupationSearch_MatchesCodeAndName_AndProfileStoresCodeOnly()
    {
        var harness = new WorkforceHarness();
        var rows = await harness.OfficialLookups.SearchOccupationsAsync("Aşçı", CancellationToken.None);
        Assert.True(rows.IsSuccess);
        Assert.Contains(rows.Value, item => item.Code == "5120.10");
        Assert.Contains(rows.Value, item => item.Code == "3434.01");

        var byCode = await harness.OfficialLookups.SearchOccupationsAsync("5120.10", CancellationToken.None);
        Assert.Contains(byCode.Value!, item => item.Code == "5120.10");

        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(officialProfile: harness.OfficialWrite(occupation: "5120.10")),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);
        Assert.Equal("5120.10", harness.Store.OfficialEmploymentProfiles.Single().OccupationCode);
        Assert.Null(typeof(Position).GetProperty("OccupationCode"));
    }

    [Fact]
    public async Task DutyCode_AcceptsSixChoices_RejectsInvalid_AndIsOptional()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var saved = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(
                hired.Value.EmployeeId,
                harness.OfficialWrite(dutyCode: "Worker")),
            CancellationToken.None);
        Assert.True(saved.IsSuccess);
        Assert.Equal("Worker", harness.Store.OfficialEmploymentProfiles.Single().DutyCode);

        var invalid = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(
                hired.Value.EmployeeId,
                harness.OfficialWrite(dutyCode: "Manager")),
            CancellationToken.None);
        Assert.False(invalid.IsSuccess);
        Assert.Equal("invalid-duty-code", invalid.Error!.Code);

        var blank = await harness.SaveOfficial.ExecuteAsync(
            new SaveOfficialEmploymentProfileCommand(
                hired.Value.EmployeeId,
                OfficialEmploymentWriteModel.Empty),
            CancellationToken.None);
        Assert.True(blank.IsSuccess);
        Assert.Null(harness.Store.OfficialEmploymentProfiles.Single().DutyCode);
        Assert.Null(typeof(Position).GetProperty("DutyCode"));
    }

    [Fact]
    public async Task IskurAndContract_ConditionalFieldsAndIncentiveRange()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var missingEnd = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value.EmployeeId,
                "Ayşe",
                "Yılmaz",
                WorkforceHarness.EmptyProfile(),
                true,
                OfficialEmploymentWriteModel.Empty,
                new EmploymentWorkforceWriteModel(
                    EmploymentContractType.FixedTerm,
                    null,
                    null,
                    IskurStatus.Normal,
                    null,
                    null,
                    IskurWorkforceStatus.FixedTerm,
                    null,
                    null)),
            CancellationToken.None);
        Assert.False(missingEnd.IsSuccess);
        Assert.Equal("contract-end-date-required", missingEnd.Error!.Code);

        var missingHours = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value.EmployeeId,
                "Ayşe",
                "Yılmaz",
                WorkforceHarness.EmptyProfile(),
                true,
                OfficialEmploymentWriteModel.Empty,
                new EmploymentWorkforceWriteModel(
                    EmploymentContractType.PartTime,
                    null,
                    null,
                    null,
                    null,
                    null,
                    IskurWorkforceStatus.PartTime,
                    null,
                    null)),
            CancellationToken.None);
        Assert.False(missingHours.IsSuccess);
        Assert.Equal("part-time-hours-required", missingHours.Error!.Code);

        var inverted = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value.EmployeeId,
                "Ayşe",
                "Yılmaz",
                WorkforceHarness.EmptyProfile(),
                true,
                OfficialEmploymentWriteModel.Empty,
                new EmploymentWorkforceWriteModel(
                    EmploymentContractType.Indefinite,
                    null,
                    null,
                    IskurStatus.FormerConvict,
                    new DateOnly(2026, 8, 24),
                    new DateOnly(2026, 8, 1),
                    IskurWorkforceStatus.FormerConvict,
                    null,
                    null)),
            CancellationToken.None);
        Assert.False(inverted.IsSuccess);
        Assert.Equal("incentive-range-invalid", inverted.Error!.Code);

        var ok = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value.EmployeeId,
                "Ayşe",
                "Yılmaz",
                WorkforceHarness.EmptyProfile(),
                true,
                OfficialEmploymentWriteModel.Empty,
                new EmploymentWorkforceWriteModel(
                    EmploymentContractType.FixedTerm,
                    new DateOnly(2026, 12, 31),
                    null,
                    IskurStatus.TerrorVictim,
                    new DateOnly(2026, 1, 1),
                    new DateOnly(2026, 12, 31),
                    IskurWorkforceStatus.TerrorVictim,
                    null,
                    null)),
            CancellationToken.None);
        Assert.True(ok.IsSuccess);
        var employment = harness.Store.Employments.Single(item => item.Id == hired.Value.EmploymentId);
        Assert.Equal(EmploymentContractType.FixedTerm, employment.ContractType);
        Assert.Equal(new DateOnly(2026, 12, 31), employment.ContractEndDate);
        Assert.Equal(IskurWorkforceStatus.TerrorVictim, employment.IskurWorkforceStatus);
    }

    [Fact]
    public async Task Bes_IsConfigurationOnly_AndRejectsInvalidRates()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);

        var over = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId,
                "Ayşe",
                "Yılmaz",
                WorkforceHarness.EmptyProfile(),
                true,
                OfficialEmploymentWriteModel.Empty,
                EmploymentWorkforceWriteModel.Empty,
                new EmploymentBesWriteModel(true, 120m, 10m)),
            CancellationToken.None);
        Assert.False(over.IsSuccess);
        Assert.Equal("bes-rate-invalid", over.Error!.Code);

        var negative = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value.EmployeeId,
                "Ayşe",
                "Yılmaz",
                WorkforceHarness.EmptyProfile(),
                true,
                OfficialEmploymentWriteModel.Empty,
                EmploymentWorkforceWriteModel.Empty,
                new EmploymentBesWriteModel(true, 3m, -1m)),
            CancellationToken.None);
        Assert.False(negative.IsSuccess);
        Assert.Equal("bes-extra-amount-invalid", negative.Error!.Code);

        var ok = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value.EmployeeId,
                "Ayşe",
                "Yılmaz",
                WorkforceHarness.EmptyProfile(),
                true,
                OfficialEmploymentWriteModel.Empty,
                EmploymentWorkforceWriteModel.Empty,
                new EmploymentBesWriteModel(true, 3m, 150m)),
            CancellationToken.None);
        Assert.True(ok.IsSuccess);
        var settings = harness.Store.EmploymentBesSettings.Single();
        Assert.True(settings.DeductionEnabled);
        Assert.Equal(3m, settings.RatePercent);
        var typeNames = typeof(Employee).Assembly.GetTypes().Select(type => type.Name).ToArray();
        Assert.DoesNotContain("PayrollRun", typeNames);
        Assert.DoesNotContain("Payslip", typeNames);
        Assert.DoesNotContain("AgiSettings", typeNames);
    }

    [Fact]
    public async Task SocialEducationAndNationality_SaveAndValidate()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);

        var missingExemption = Profile(
            military: MilitaryServiceStatus.Exempt,
            nationality: "TR");
        var rejected = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId, "Ayşe", "Yılmaz", missingExemption, true),
            CancellationToken.None);
        Assert.False(rejected.IsSuccess);
        Assert.Equal("invalid-hr-profile", rejected.Error!.Code);
        Assert.Equal(
            [HrValidation.Codes.MilitaryExemptionReasonRequired],
            rejected.Error.Errors![HrValidation.Fields.MilitaryExemptionReason]);

        var missingDeferral = Profile(
            military: MilitaryServiceStatus.Deferred,
            nationality: "tr");
        var rejectedDeferral = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value.EmployeeId, "Ayşe", "Yılmaz", missingDeferral, true),
            CancellationToken.None);
        Assert.False(rejectedDeferral.IsSuccess);
        Assert.Equal(
            [HrValidation.Codes.MilitaryDefermentReasonRequired],
            rejectedDeferral.Error!.Errors![HrValidation.Fields.MilitaryDefermentReason]);

        var invalidNationality = Profile(nationality: "Türkiye");
        var nationalityRejected = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value.EmployeeId, "Ayşe", "Yılmaz", invalidNationality, true),
            CancellationToken.None);
        Assert.False(nationalityRejected.IsSuccess);
        Assert.Equal(
            [HrValidation.Codes.InvalidNationality],
            nationalityRejected.Error!.Errors![HrValidation.Fields.Nationality]);

        var permit = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value.EmployeeId,
                "Ayşe",
                "Yılmaz",
                Profile(
                    nationality: "de",
                    military: MilitaryServiceStatus.Completed,
                    education: EducationLevel.Bachelor,
                    school: "ODTÜ"),
                true,
                OfficialEmploymentWriteModel.Empty,
                new EmploymentWorkforceWriteModel(
                    null, null, null, null, null, null, null,
                    new DateOnly(2026, 6, 1),
                    new DateOnly(2026, 5, 1))),
            CancellationToken.None);
        Assert.False(permit.IsSuccess);
        Assert.Equal("work-permit-range-invalid", permit.Error!.Code);

        var ok = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value.EmployeeId,
                "Ayşe",
                "Yılmaz",
                Profile(
                    nationality: "de",
                    military: MilitaryServiceStatus.Exempt,
                    exemption: "Sağlık",
                    education: EducationLevel.Bachelor,
                    school: "ODTÜ",
                    language: ForeignLanguageSummary.English,
                    licence: DrivingLicenceCategory.B),
                true,
                OfficialEmploymentWriteModel.Empty,
                new EmploymentWorkforceWriteModel(
                    EmploymentContractType.Indefinite,
                    null,
                    null,
                    IskurStatus.Normal,
                    null,
                    null,
                    IskurWorkforceStatus.Indefinite,
                    new DateOnly(2026, 1, 1),
                    new DateOnly(2026, 12, 31))),
            CancellationToken.None);
        Assert.True(ok.IsSuccess);

        var card = await harness.HrCard.ExecuteAsync(hired.Value.EmployeeId, true, CancellationToken.None);
        Assert.True(card.IsSuccess);
        Assert.Equal("DE", card.Value.Profile.Nationality);
        Assert.Equal(MilitaryServiceStatus.Exempt, card.Value.Profile.MilitaryServiceStatus);
        Assert.Equal("Sağlık", card.Value.Profile.MilitaryExemptionReason);
        Assert.Null(card.Value.Profile.MilitaryDefermentReason);
        Assert.Equal(EducationLevel.Bachelor, card.Value.Profile.EducationLevel);
        Assert.Equal("ODTÜ", card.Value.Profile.SchoolName);
        Assert.Equal(ForeignLanguageSummary.English, card.Value.Profile.ForeignLanguage);
        Assert.Equal(DrivingLicenceCategory.B, card.Value.Profile.DrivingLicenceCategory);
        Assert.Null(typeof(EmployeeHrProfile).GetProperty("MothersMaidenName"));
        Assert.Equal(249, Iso3166Alpha2Catalog.Codes.Count);
        Assert.Contains("TR", Iso3166Alpha2Catalog.Codes);
        Assert.Contains("GB", Iso3166Alpha2Catalog.Codes);
        Assert.True(Iso3166Alpha2Catalog.TryNormalize("tr", out var normalized, out _));
        Assert.Equal("TR", normalized);
    }

    [Fact]
    public async Task BlankOfficialSocialAndEducation_RemainValidOnOrdinarySave()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        var saved = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId,
                "Ayşe",
                "Yılmaz",
                WorkforceHarness.EmptyProfile(),
                true),
            CancellationToken.None);
        Assert.True(saved.IsSuccess);
        Assert.Empty(harness.Store.OfficialEmploymentProfiles);
        Assert.Empty(harness.Store.EmploymentBesSettings);
    }

    private static HrProfileWriteModel Profile(
        string? nationality = null,
        MilitaryServiceStatus? military = null,
        string? exemption = null,
        string? deferment = null,
        EducationLevel? education = null,
        string? school = null,
        ForeignLanguageSummary? language = null,
        DrivingLicenceCategory? licence = null) =>
        new(
            null, null, nationality, null, null, null, null, null, education,
            null, null, null, null, null, null, null, null,
            licence, military, exemption, deferment, null, null, school, null, language, null, []);

    private static string CataloguePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "data", "reference", "sgk-occupation-codes.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Occupation catalogue artifact was not found.");
    }
}
