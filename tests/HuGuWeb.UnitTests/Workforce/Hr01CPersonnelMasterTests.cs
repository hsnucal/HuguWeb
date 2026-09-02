using ClosedXML.Excel;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using HuGuWeb.Workforce.Infrastructure.Spreadsheet;

namespace HuGuWeb.UnitTests.Workforce;

public class Hr01CPersonnelMasterTests
{
    [Fact]
    public async Task PaymentProfile_NormalizesIban()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var useCase = new SaveEmployeePaymentProfileUseCase(harness.Store, harness.Workplace);
        var saved = await useCase.ExecuteAsync(
            new SaveEmployeePaymentProfileCommand(
                hired.Value.EmployeeId,
                "TR33 0006 1005 1978 6457 8413 26",
                "Test Bank",
                CanWriteSensitive: true),
            CancellationToken.None);

        Assert.True(saved.IsSuccess);
        Assert.Equal("TR330006100519786457841326", saved.Value.Iban);
    }

    [Fact]
    public async Task PaymentProfile_RejectsInvalidIban()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True(hired.IsSuccess);
        var useCase = new SaveEmployeePaymentProfileUseCase(harness.Store, harness.Workplace);
        var saved = await useCase.ExecuteAsync(
            new SaveEmployeePaymentProfileCommand(hired.Value.EmployeeId, "BAD", null, CanWriteSensitive: true),
            CancellationToken.None);

        Assert.False(saved.IsSuccess);
        Assert.Equal("payment-profile-invalid-iban", saved.Error!.Code);
    }

    [Fact]
    public async Task PaymentProfile_RejectsNonTurkishIbanPrefix()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True(hired.IsSuccess);
        var useCase = new SaveEmployeePaymentProfileUseCase(harness.Store, harness.Workplace);
        var saved = await useCase.ExecuteAsync(
            new SaveEmployeePaymentProfileCommand(
                hired.Value.EmployeeId,
                "DE89370400440532013000",
                null,
                CanWriteSensitive: true),
            CancellationToken.None);

        Assert.False(saved.IsSuccess);
        Assert.Equal("payment-profile-invalid-iban", saved.Error!.Code);
    }

    [Fact]
    public async Task ProfileUpdate_RecordsHistory()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(profile: new HrProfileWriteModel(
                null, null, null, null, null, null, null, null, null,
                "5551112233", null, "old@example.com",
                null, null, null, null, null,
                null, null, null, null, null, null, null, null, null, [])),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var actor = new PersonnelChangeContext("user-1", null, harness.OrganizationId, harness.PropertyId, harness.Clock.UtcNow);
        var updated = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value.EmployeeId,
                "Ayşe",
                "Yılmaz",
                new HrProfileWriteModel(
                    null, null, null, null, null, null, null, null, null,
                    "5559998877", null, "new@example.com",
                    null, null, null, null, null,
                    null, null, null, null, null, null, null, null, null, []),
                true,
                ChangeContext: actor),
            CancellationToken.None);
        Assert.True(updated.IsSuccess);

        var history = await new PersonnelProfileHistoryQuery(harness.Store, harness.Workplace)
            .ExecuteAsync(hired.Value.EmployeeId, true, CancellationToken.None);
        Assert.True(history.IsSuccess);
        Assert.Contains(history.Value!, item => item.FieldCode == PersonnelProfileFieldCodes.MobilePhone);
        Assert.Contains(history.Value!, item => item.FieldCode == PersonnelProfileFieldCodes.Email);
    }

    [Fact]
    public void SpreadsheetSafety_PrefixesFormulaCells()
    {
        Assert.Equal("'=CMD", SpreadsheetSafety.SanitizeCellValue("=CMD"));
        Assert.Equal("Safe", SpreadsheetSafety.SanitizeCellValue("Safe"));
    }

    [Fact]
    public void ExportWorkbook_PrefixesFormulaLikeValues()
    {
        var service = new ClosedXmlPersonnelSpreadsheetService();
        var bytes = service.BuildExportWorkbook(
            [new PersonnelExportColumn("personnelNumber", "Sicil No"), new PersonnelExportColumn("givenName", "Ad")],
            [new PersonnelExportRow(new Dictionary<string, string?> { ["personnelNumber"] = "1001", ["givenName"] = "=Evil" })]);
        Assert.NotEmpty(bytes);
        Assert.Equal("'=Evil", SpreadsheetSafety.SanitizeCellValue("=Evil"));
    }

    [Fact]
    public void ExportWorkbook_EnablesAutoFilterAndPreservesValues()
    {
        var service = new ClosedXmlPersonnelSpreadsheetService();
        var bytes = service.BuildExportWorkbook(
        [
            new PersonnelExportColumn("personnelNumber", "Sicil No"),
            new PersonnelExportColumn("givenName", "Ad"),
            new PersonnelExportColumn("startDate", "İşe Giriş Tarihi"),
        ],
        [
            new PersonnelExportRow(new Dictionary<string, string?>
            {
                ["personnelNumber"] = "P-1001",
                ["givenName"] = "Ayşe",
                ["startDate"] = "2026-01-15",
            }),
        ]);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);
        Assert.True(sheet.AutoFilter.IsEnabled);
        Assert.NotNull(sheet.AutoFilter.Range);
        Assert.Equal(3, sheet.AutoFilter.Range.ColumnCount());
        Assert.Equal(1, sheet.SheetView.SplitRow);
        Assert.Equal("Sicil No", sheet.Cell(1, 1).GetString());
        Assert.Equal("P-1001", sheet.Cell(2, 1).GetFormattedString());
        Assert.Equal("@", sheet.Cell(2, 1).Style.NumberFormat.Format);
        Assert.Equal(XLDataType.Text, sheet.Cell(2, 1).DataType);
        Assert.Equal("Ayşe", sheet.Cell(2, 2).GetString());
        Assert.True(sheet.Row(1).Style.Font.Bold);
        Assert.True(sheet.Cell(1, 1).Style.Font.Bold);
        Assert.Equal(XLColor.White, sheet.Row(1).Style.Font.FontColor);
        Assert.Equal(XLColor.White, sheet.Cell(1, 1).Style.Font.FontColor);
        Assert.Equal(XLColor.FromHtml("#862A51"), sheet.Row(1).Style.Fill.BackgroundColor);
        Assert.Equal(XLColor.FromHtml("#862A51"), sheet.Cell(1, 1).Style.Fill.BackgroundColor);
        Assert.Equal(XLAlignmentVerticalValues.Center, sheet.Row(1).Style.Alignment.Vertical);
    }

    [Fact]
    public void ExportWorkbook_SizesColumnsToContentWithBounds()
    {
        var service = new ClosedXmlPersonnelSpreadsheetService();
        var longEmail = new string('a', 80) + "@hotel.example";
        var bytes = service.BuildExportWorkbook(
        [
            new PersonnelExportColumn("personnelNumber", "Sicil No"),
            new PersonnelExportColumn("position", "Pozisyon"),
            new PersonnelExportColumn("email", "E-posta"),
        ],
        [
            new PersonnelExportRow(new Dictionary<string, string?>
            {
                ["personnelNumber"] = "P-1003",
                ["position"] = "Resepsiyon Görevlisi",
                ["email"] = longEmail,
            }),
        ]);

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet(1);
        Assert.True(sheet.Column(2).Width > sheet.Column(1).Width);
        Assert.InRange(sheet.Column(1).Width, 10d, 50d);
        Assert.InRange(sheet.Column(2).Width, 10d, 50d);
        Assert.Equal(50d, sheet.Column(3).Width);
    }

    [Fact]
    public async Task Export_RejectsSensitiveColumnsWithoutPermission()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var export = new PersonnelExcelExportUseCase(
            harness.Store,
            harness.Clock,
            harness.Workplace,
            new ClosedXmlPersonnelSpreadsheetService());
        var result = await export.ExecuteAsync(
            new PersonnelExportQuery(
                CanReadSensitive: false,
                VisibleColumns: ["personnelNumber", "nationalIdentityNumber", "paymentIban"]),
            CancellationToken.None);
        Assert.True(result.IsSuccess);

        using var workbook = new XLWorkbook(new MemoryStream(result.Value!));
        var headers = workbook.Worksheet(1).Row(1).CellsUsed().Select(item => item.GetString()).ToArray();
        Assert.DoesNotContain(headers, item => item.Contains("Kimlik", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(headers, item => item.Contains("IBAN", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportConfirm_RollsBackAllRowsWhenLaterPersistenceFails()
    {
        var harness = new WorkforceHarness();
        var import = CreateImportUseCase(harness);
        var actor = CreateActor(harness, "user-1");
        var initialCount = harness.Store.Employees.Count;

        SeedPreview(harness, import, actor, [
            CreateRow(2, null, "Ali", "Veli", "Kat Hizmetleri", "Kat Görevlisi"),
            CreateRow(3, null, "Ayşe", "Demir", "Kat Hizmetleri", "Kat Görevlisi"),
            CreateRow(4, null, "Mehmet", "Kaya", "Kat Hizmetleri", "Kat Görevlisi"),
        ], out var token);

        harness.Store.FailSaveChangesAfterCount = 3;
        var confirmed = await import.ConfirmAsync(
            new PersonnelImportConfirmCommand(token, actor, CanWriteSensitive: true),
            CancellationToken.None);

        Assert.False(confirmed.IsSuccess);
        Assert.Equal("personnel-import-failed", confirmed.Error!.Code);
        Assert.Equal(initialCount, harness.Store.Employees.Count);
        Assert.Empty(harness.Store.ImportRuns);
    }

    [Fact]
    public async Task ImportPreview_ForbidsAnotherUserConfirmation()
    {
        var harness = new WorkforceHarness();
        var import = CreateImportUseCase(harness);
        var owner = CreateActor(harness, "owner");
        SeedPreview(harness, import, owner, [
            CreateRow(2, null, "Ali", "Veli", "Kat Hizmetleri", "Kat Görevlisi"),
        ], out var token);

        var other = CreateActor(harness, "other-user");
        var confirmed = await import.ConfirmAsync(
            new PersonnelImportConfirmCommand(token, other, CanWriteSensitive: true),
            CancellationToken.None);

        Assert.False(confirmed.IsSuccess);
        Assert.Equal("personnel-import-preview-forbidden", confirmed.Error!.Code);
    }

    [Fact]
    public async Task ImportPreview_ExpiresAfterTtl()
    {
        var harness = new WorkforceHarness();
        var time = new FakeTimeProvider(harness.Clock.UtcNow);
        var import = CreateImportUseCase(harness, time);
        var actor = CreateActor(harness, "user-1");
        SeedPreview(harness, import, actor, [
            CreateRow(2, null, "Ali", "Veli", "Kat Hizmetleri", "Kat Görevlisi"),
        ], out var token);

        time.Advance(TimeSpan.FromMinutes(31));
        var confirmed = await import.ConfirmAsync(
            new PersonnelImportConfirmCommand(token, actor, CanWriteSensitive: true),
            CancellationToken.None);

        Assert.False(confirmed.IsSuccess);
        Assert.Equal("personnel-import-preview-expired", confirmed.Error!.Code);
    }

    [Fact]
    public async Task ImportPreview_RejectsInvalidDepartmentPositionMapping()
    {
        var harness = new WorkforceHarness();
        var import = CreateImportUseCase(harness);
        var workbook = BuildWorkbook([
            CreateRow(2, null, "Ali", "Veli", "Kat Hizmetleri", "Resepsiyon Görevlisi"),
        ]);
        await using var stream = new MemoryStream(workbook);
        var preview = await import.PreviewAsync(
            new PersonnelImportPreviewCommand(stream, workbook.Length, "import.xlsx", true, "user-1"),
            CancellationToken.None);

        Assert.True(preview.IsSuccess);
        Assert.Equal(1, preview.Value!.InvalidCount);
        Assert.False(preview.Value.CanConfirm);
    }

    [Fact]
    public async Task ImportConfirm_RevalidatesDepartmentPositionMapping()
    {
        var harness = new WorkforceHarness();
        var import = CreateImportUseCase(harness);
        var actor = CreateActor(harness, "user-1");
        SeedPreview(harness, import, actor, [
            CreateRow(2, null, "Ali", "Veli", "Kat Hizmetleri", "Kat Görevlisi"),
        ], out var token);

        harness.Store.Applicabilities.RemoveAll(item =>
            item.DepartmentId == harness.DepartmentId && item.PositionId == harness.PositionId);

        var confirmed = await import.ConfirmAsync(
            new PersonnelImportConfirmCommand(token, actor, CanWriteSensitive: true),
            CancellationToken.None);

        Assert.False(confirmed.IsSuccess);
        Assert.Equal("personnel-import-failed", confirmed.Error!.Code);
    }

    [Fact]
    public async Task ImportCreate_AllocatesPersonnelNumberWhenBlank()
    {
        var harness = new WorkforceHarness();
        var import = CreateImportUseCase(harness);
        var actor = CreateActor(harness, "user-1");
        SeedPreview(harness, import, actor, [
            CreateRow(2, null, "Ali", "Veli", "Kat Hizmetleri", "Kat Görevlisi"),
        ], out var token);

        var confirmed = await import.ConfirmAsync(
            new PersonnelImportConfirmCommand(token, actor, CanWriteSensitive: true),
            CancellationToken.None);

        Assert.True(confirmed.IsSuccess);
        Assert.Equal(1, confirmed.Value!.CreatedCount);
        Assert.Single(harness.Store.Employees, item => item.GivenName == "Ali" && !string.IsNullOrWhiteSpace(item.PersonnelNumber));
    }

    [Fact]
    public async Task ImportUpdate_MatchesExistingPersonnelNumberWithoutDuplicate()
    {
        var harness = new WorkforceHarness();
        var employee = harness.SeedEmployee("2001", "Existing", "Person");
        var employment = Employment.Open(Guid.CreateVersion7(), employee.Id, harness.Clock.Today, harness.Clock.Today);
        harness.Store.Employments.Add(employment);
        harness.Store.Assignments.Add(Assignment.StartPrimary(
            Guid.CreateVersion7(),
            employment.Id,
            harness.DepartmentId,
            harness.PositionId,
            harness.Clock.Today));

        var import = CreateImportUseCase(harness);
        var actor = CreateActor(harness, "user-1");
        SeedPreview(harness, import, actor, [
            CreateRow(2, "2001", "Existing", "Updated", "Kat Hizmetleri", "Kat Görevlisi"),
        ], out var token);

        var confirmed = await import.ConfirmAsync(
            new PersonnelImportConfirmCommand(token, actor, CanWriteSensitive: true),
            CancellationToken.None);

        Assert.True(confirmed.IsSuccess);
        Assert.Equal(1, confirmed.Value!.UpdatedCount);
        Assert.Equal(0, confirmed.Value.CreatedCount);
        Assert.Single(harness.Store.Employees, item => item.PersonnelNumber == "2001");
        Assert.Equal("Updated", harness.Store.Employees.Single(item => item.PersonnelNumber == "2001").FamilyName);
        Assert.Equal("2001", harness.Store.Employees.Single(item => item.PersonnelNumber == "2001").PersonnelNumber);
    }

    [Fact]
    public void ImportTemplate_UsesBrandHeaderAndCatalogColumns()
    {
        var service = new ClosedXmlPersonnelSpreadsheetService();
        var bytes = service.BuildImportTemplate(new PersonnelImportTemplateContext(
            [new PersonnelImportCodeName("HK", "Kat Hizmetleri")],
            [new PersonnelImportCodeName("KG", "Kat Görevlisi")]));

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheet("Personnel");
        var headers = sheet.Row(1).CellsUsed().Select(item => item.GetString()).ToArray();
        Assert.Equal(PersonnelImportColumnCatalog.Columns.Count, headers.Length);
        foreach (var column in PersonnelImportColumnCatalog.Columns)
        {
            Assert.Contains(PersonnelImportColumnCatalog.DisplayHeader(column), headers);
        }

        Assert.Contains(headers, item => item.EndsWith(" *", StringComparison.Ordinal));
        Assert.Contains("Ad *", headers);
        Assert.Contains("Soyad *", headers);
        Assert.Contains("Departman Kodu *", headers);
        Assert.Contains("Pozisyon Kodu *", headers);
        Assert.Contains("İşe Giriş Tarihi *", headers);
        Assert.DoesNotContain(headers, item => item.Contains("GUID", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(headers, item => item.Contains("EmployeeId", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(headers, item => item.Contains("OrganizationId", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(headers, item => item.Contains("PropertyId", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(headers, item => item.Contains("Salary", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(headers, item => item.Contains("Maaş", StringComparison.OrdinalIgnoreCase));

        Assert.True(sheet.AutoFilter.IsEnabled);
        Assert.Equal(1, sheet.SheetView.SplitRow);
        Assert.True(sheet.Row(1).Style.Font.Bold);
        Assert.Equal(XLColor.White, sheet.Row(1).Style.Font.FontColor);
        Assert.Equal(XLColor.FromHtml("#862A51"), sheet.Row(1).Style.Fill.BackgroundColor);
        foreach (var column in sheet.ColumnsUsed())
        {
            Assert.InRange(column.Width, 10d, 48d);
        }

        Assert.NotNull(workbook.Worksheets.FirstOrDefault(item => item.Name == "Yardım"));
        Assert.NotNull(workbook.Worksheets.FirstOrDefault(item => item.Name == "Referans"));
        var meta = workbook.Worksheets.First(item => item.Name == "_meta");
        Assert.Equal(PersonnelImportColumnCatalog.WorkbookVersion, meta.Cell(1, 1).GetString());
        Assert.Equal(PersonnelImportColumnCatalog.Ids.GivenName, meta.Cell(2, 2).GetString());
    }

    [Fact]
    public async Task ImportPreview_RejectsInvalidOptionalField()
    {
        var harness = new WorkforceHarness();
        var import = CreateImportUseCase(harness);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Personnel");
        sheet.Cell(1, 1).Value = "Ad";
        sheet.Cell(1, 2).Value = "Soyad";
        sheet.Cell(1, 3).Value = "Departman";
        sheet.Cell(1, 4).Value = "Pozisyon";
        sheet.Cell(1, 5).Value = "İşe Giriş Tarihi";
        sheet.Cell(1, 6).Value = "Öğrenim Durumu";
        sheet.Cell(2, 1).Value = "Ali";
        sheet.Cell(2, 2).Value = "Veli";
        sheet.Cell(2, 3).Value = "Kat Hizmetleri";
        sheet.Cell(2, 4).Value = "Kat Görevlisi";
        sheet.Cell(2, 5).Value = "2026-01-15";
        sheet.Cell(2, 6).Value = "NotALevel";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();
        await using var input = new MemoryStream(bytes);
        var preview = await import.PreviewAsync(
            new PersonnelImportPreviewCommand(input, bytes.Length, "import.xlsx", true, "user-1"),
            CancellationToken.None);

        Assert.True(preview.IsSuccess);
        Assert.Equal(1, preview.Value!.InvalidCount);
        Assert.Contains(preview.Value.Rows[0].Errors, item => item.Field == "educationLevel");
    }

    [Fact]
    public async Task ImportPreview_RejectsPaymentFieldsWithoutSensitivePermission()
    {
        var harness = new WorkforceHarness();
        var import = CreateImportUseCase(harness);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Personnel");
        sheet.Cell(1, 1).Value = "Ad";
        sheet.Cell(1, 2).Value = "Soyad";
        sheet.Cell(1, 3).Value = "Departman";
        sheet.Cell(1, 4).Value = "Pozisyon";
        sheet.Cell(1, 5).Value = "İşe Giriş Tarihi";
        sheet.Cell(1, 6).Value = "IBAN";
        sheet.Cell(2, 1).Value = "Ali";
        sheet.Cell(2, 2).Value = "Veli";
        sheet.Cell(2, 3).Value = "Kat Hizmetleri";
        sheet.Cell(2, 4).Value = "Kat Görevlisi";
        sheet.Cell(2, 5).Value = "2026-01-15";
        sheet.Cell(2, 6).Value = "TR330006100519786457841326";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();
        await using var input = new MemoryStream(bytes);
        var preview = await import.PreviewAsync(
            new PersonnelImportPreviewCommand(input, bytes.Length, "import.xlsx", false, "user-1"),
            CancellationToken.None);

        Assert.True(preview.IsSuccess);
        Assert.Equal(1, preview.Value!.InvalidCount);
        Assert.Contains(preview.Value.Rows[0].Errors, item => item.Field == "sensitive");
    }

    [Fact]
    public async Task ImportConfirm_PersistsSupportedOptionalProfileFields()
    {
        var harness = new WorkforceHarness();
        var import = CreateImportUseCase(harness);
        var actor = CreateActor(harness, "user-1");
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Personnel");
        sheet.Cell(1, 1).Value = "Ad *";
        sheet.Cell(1, 2).Value = "Soyad *";
        sheet.Cell(1, 3).Value = "Departman";
        sheet.Cell(1, 4).Value = "Pozisyon";
        sheet.Cell(1, 5).Value = "İşe Giriş Tarihi *";
        sheet.Cell(1, 6).Value = "Cinsiyet";
        sheet.Cell(1, 7).Value = "Ev Telefonu";
        sheet.Cell(1, 8).Value = "Not";
        sheet.Cell(2, 1).Value = "Ali";
        sheet.Cell(2, 2).Value = "Veli";
        sheet.Cell(2, 3).Value = "Kat Hizmetleri";
        sheet.Cell(2, 4).Value = "Kat Görevlisi";
        sheet.Cell(2, 5).Value = "2026-01-15";
        sheet.Cell(2, 6).Value = "Male";
        sheet.Cell(2, 7).Value = "5551112233";
        sheet.Cell(2, 8).Value = "Excel note";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var bytes = stream.ToArray();
        await using var input = new MemoryStream(bytes);
        var preview = await import.PreviewAsync(
            new PersonnelImportPreviewCommand(input, bytes.Length, "import.xlsx", true, actor.UserId),
            CancellationToken.None);
        Assert.True(preview.IsSuccess);
        Assert.True(preview.Value!.CanConfirm);

        var confirmed = await import.ConfirmAsync(
            new PersonnelImportConfirmCommand(preview.Value.PreviewToken, actor, true),
            CancellationToken.None);
        Assert.True(confirmed.IsSuccess);
        var employee = harness.Store.Employees.Single(item => item.GivenName == "Ali");
        var profile = harness.Store.HrProfiles.Single(item => item.EmployeeId == employee.Id);
        Assert.Equal(Gender.Male, profile.Gender);
        Assert.Equal("5551112233", profile.HomePhone);
        Assert.Equal("Excel note", profile.HrNotes);
    }

    [Fact]
    public void ProfileHistory_MasksSensitiveValues()
    {
        var masked = SensitiveValueMasker.MaskForHistory(
            PersonnelProfileFieldCodes.NationalIdentityNumber,
            "12345678901");
        Assert.Equal("*******8901", masked);
        var iban = SensitiveValueMasker.MaskForHistory(
            PersonnelProfileFieldCodes.PaymentIban,
            "TR330006100519786457841326");
        Assert.EndsWith("1326", iban!);
        Assert.Contains('*', iban!);
    }

    private static PersonnelExcelImportUseCase CreateImportUseCase(
        WorkforceHarness harness,
        TimeProvider? time = null) =>
        new(
            harness.Store,
            harness.Workplace,
            new ClosedXmlPersonnelSpreadsheetService(),
            new PersonnelImportPreviewStore(time ?? TimeProvider.System),
            new HireEmployeeWithProfileUseCase(harness.Store, harness.Clock, harness.Workplace),
            new UpdateEmployeeHrProfileUseCase(harness.Store, harness.Clock, harness.Workplace),
            new SaveEmployeePaymentProfileUseCase(harness.Store, harness.Workplace),
            time ?? TimeProvider.System);

    private static PersonnelChangeContext CreateActor(WorkforceHarness harness, string userId) =>
        new(userId, null, harness.OrganizationId, harness.PropertyId, harness.Clock.UtcNow);

    private static void SeedPreview(
        WorkforceHarness harness,
        PersonnelExcelImportUseCase import,
        PersonnelChangeContext actor,
        IReadOnlyList<ImportTestRow> rows,
        out string token)
    {
        var workbook = BuildWorkbook(rows);
        using var stream = new MemoryStream(workbook);
        var preview = import.PreviewAsync(
            new PersonnelImportPreviewCommand(stream, workbook.Length, "import.xlsx", true, actor.UserId),
            CancellationToken.None).GetAwaiter().GetResult();
        Assert.True(preview.IsSuccess);
        Assert.True(preview.Value!.CanConfirm);
        token = preview.Value.PreviewToken;
    }

    private static byte[] BuildWorkbook(IReadOnlyList<ImportTestRow> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Personnel");
        sheet.Cell(1, 1).Value = "Sicil No";
        sheet.Cell(1, 2).Value = "Ad";
        sheet.Cell(1, 3).Value = "Soyad";
        sheet.Cell(1, 4).Value = "Departman";
        sheet.Cell(1, 5).Value = "Pozisyon";
        sheet.Cell(1, 6).Value = "İşe Giriş Tarihi";

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var line = index + 2;
            sheet.Cell(line, 1).Value = row.PersonnelNumber ?? string.Empty;
            sheet.Cell(line, 2).Value = row.GivenName;
            sheet.Cell(line, 3).Value = row.FamilyName;
            sheet.Cell(line, 4).Value = row.DepartmentName;
            sheet.Cell(line, 5).Value = row.PositionName;
            sheet.Cell(line, 6).Value = row.StartDate.ToString("yyyy-MM-dd");
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static ImportTestRow CreateRow(
        int rowNumber,
        string? personnelNumber,
        string givenName,
        string familyName,
        string departmentName,
        string positionName) =>
        new(rowNumber, personnelNumber, givenName, familyName, departmentName, positionName, new DateOnly(2026, 1, 15));

    private sealed record ImportTestRow(
        int RowNumber,
        string? PersonnelNumber,
        string GivenName,
        string FamilyName,
        string DepartmentName,
        string PositionName,
        DateOnly StartDate);
}

internal sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _utcNow = start;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
}
