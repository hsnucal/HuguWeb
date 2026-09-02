using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class PersonnelExcelImportUseCase(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext,
    IPersonnelSpreadsheetService spreadsheet,
    PersonnelImportPreviewStore previewStore,
    HireEmployeeWithProfileUseCase hire,
    UpdateEmployeeHrProfileUseCase update,
    SaveEmployeePaymentProfileUseCase payment,
    TimeProvider time)
{
    public async Task<WorkforceResult<byte[]>> BuildTemplateAsync(CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var departments = await store.ListDepartmentsAsync(workplace.Value.Property.Id, cancellationToken);
        var positions = await store.ListPositionsAsync(workplace.Value.Property.Id, cancellationToken);
        return spreadsheet.BuildImportTemplate(new PersonnelImportTemplateContext(
            departments.Where(item => item.IsActive)
                .Select(item => new PersonnelImportCodeName(item.Code, item.Name))
                .ToArray(),
            positions.Where(item => item.IsActive)
                .Select(item => new PersonnelImportCodeName(item.Code, item.Name))
                .ToArray()));
    }

    public async Task<WorkforceResult<PersonnelImportPreviewResult>> PreviewAsync(
        PersonnelImportPreviewCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ContentLength <= 0)
        {
            return WorkforceError.PersonnelImportInvalidFile("Workbook is empty.");
        }

        if (command.ContentLength > PersonnelImportLimits.MaxWorkbookBytes)
        {
            return WorkforceError.PersonnelImportTooLarge(
                $"Workbook exceeds {PersonnelImportLimits.MaxWorkbookBytes / (1024 * 1024)} MB.");
        }

        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        await using var stream = command.Content;
        var parsed = spreadsheet.ParseImportWorkbook(stream, command.ContentLength);
        if (!parsed.IsSuccess)
        {
            return WorkforceError.PersonnelImportInvalidFile("Workbook could not be parsed.");
        }

        if (parsed.Rows.Count > PersonnelImportLimits.MaxRows)
        {
            return WorkforceError.PersonnelImportTooLarge(
                $"Workbook exceeds {PersonnelImportLimits.MaxRows} rows.");
        }

        var context = await BuildImportContextAsync(
            workplace.Value.Organization.Id,
            workplace.Value.Property.Id,
            cancellationToken);
        var previews = new List<PersonnelImportRowPreview>();
        var validRows = new List<PersonnelImportPreviewStore.ValidatedImportRow>();
        var personnelNumbersInFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in parsed.Rows)
        {
            var preview = await ValidateRowAsync(
                raw,
                context,
                command.CanWriteSensitive,
                personnelNumbersInFile,
                cancellationToken);
            previews.Add(preview);
            if (preview.Errors.Count == 0)
            {
                var validated = await BuildValidatedRowAsync(preview, raw, context, command.CanWriteSensitive, cancellationToken);
                if (validated is not null)
                {
                    validRows.Add(validated);
                }
            }
        }

        var invalidRows = previews.Where(item => item.Errors.Count > 0).ToArray();
        var token = Guid.CreateVersion7().ToString("N");
        previewStore.Store(token, new PersonnelImportPreviewStore.StoredPreview(
            workplace.Value.Organization.Id,
            workplace.Value.Property.Id,
            command.ActorUserId,
            command.FileName,
            parsed.Rows.Count,
            validRows,
            invalidRows,
            time.GetUtcNow().AddMinutes(30),
            command.CanWriteSensitive));

        return new PersonnelImportPreviewResult(
            token,
            parsed.Rows.Count,
            validRows.Count(item => item.Action == PersonnelImportAction.Create),
            validRows.Count(item => item.Action == PersonnelImportAction.Update),
            invalidRows.Length,
            previews,
            invalidRows.Length == 0 && validRows.Count > 0);
    }

    public async Task<WorkforceResult<PersonnelImportConfirmResult>> ConfirmAsync(
        PersonnelImportConfirmCommand command,
        CancellationToken cancellationToken)
    {
        if (!previewStore.TryGet(command.PreviewToken, out var preview) || preview is null)
        {
            return WorkforceError.PersonnelImportPreviewExpired();
        }

        if (preview.InvalidRows.Count > 0)
        {
            return WorkforceError.PersonnelImportPreviewInvalid();
        }

        if (preview.ValidRows.Count == 0)
        {
            return WorkforceError.PersonnelImportPreviewInvalid();
        }

        if (!string.Equals(preview.ActorUserId, command.Actor.UserId, StringComparison.Ordinal))
        {
            return WorkforceError.PersonnelImportPreviewForbidden();
        }

        if (preview.OrganizationId != command.Actor.OrganizationId
            || preview.PropertyId != command.Actor.PropertyId)
        {
            return WorkforceError.PersonnelImportPreviewForbidden();
        }

        if (preview.CanWriteSensitive != command.CanWriteSensitive)
        {
            return WorkforceError.PersonnelImportPreviewForbidden();
        }

        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        if (workplace.Value.Organization.Id != preview.OrganizationId
            || workplace.Value.Property.Id != preview.PropertyId)
        {
            return WorkforceError.PersonnelImportPreviewForbidden();
        }

        var context = await BuildImportContextAsync(
            preview.OrganizationId,
            preview.PropertyId,
            cancellationToken);

        foreach (var row in preview.ValidRows)
        {
            var validationError = await ValidateConfirmedRowAsync(row, context, cancellationToken);
            if (validationError is not null)
            {
                return validationError;
            }
        }

        var actor = command.Actor with { ChangeSource = PersonnelChangeSources.ExcelImport };
        await using var transaction = await store.BeginTransactionAsync(cancellationToken);

        try
        {
            var results = new List<PersonnelImportRowPreview>();
            var created = 0;
            var updated = 0;

            foreach (var row in preview.ValidRows.OrderBy(item => item.RowNumber))
            {
                if (row.Action == PersonnelImportAction.Create && row.CreateCommand is not null)
                {
                    var hired = await hire.ExecuteAsync(row.CreateCommand, cancellationToken);
                    if (!hired.IsSuccess)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return WorkforceError.PersonnelImportFailed(hired.Error!.Detail);
                    }

                    created++;
                    var paymentError = await TrySavePaymentAsync(
                        hired.Value.EmployeeId,
                        row,
                        command,
                        cancellationToken);
                    if (paymentError is not null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return paymentError;
                    }

                    results.Add(new PersonnelImportRowPreview(
                        row.RowNumber,
                        row.Action,
                        hired.Value.PersonnelNumber,
                        row.CreateCommand.GivenName,
                        row.CreateCommand.FamilyName,
                        string.Empty,
                        string.Empty,
                        row.CreateCommand.EmploymentStartDate,
                        [],
                        []));
                }
                else if (row.Action == PersonnelImportAction.Update
                         && row.UpdateCommand is not null
                         && row.ExistingEmployeeId is Guid employeeId)
                {
                    var updatedResult = await update.ExecuteAsync(
                        row.UpdateCommand with
                        {
                            ChangeContext = actor
                        },
                        cancellationToken);
                    if (!updatedResult.IsSuccess)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return WorkforceError.PersonnelImportFailed(updatedResult.Error!.Detail);
                    }

                    updated++;
                    var paymentError = await TrySavePaymentAsync(
                        employeeId,
                        row,
                        command,
                        cancellationToken);
                    if (paymentError is not null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return paymentError;
                    }

                    results.Add(new PersonnelImportRowPreview(
                        row.RowNumber,
                        row.Action,
                        null,
                        row.UpdateCommand.GivenName,
                        row.UpdateCommand.FamilyName,
                        string.Empty,
                        string.Empty,
                        DateOnly.FromDateTime(DateTime.UtcNow),
                        row.ChangedFields,
                        []));
                }
            }

            store.AddPersonnelImportRun(PersonnelImportRun.Create(
                Guid.CreateVersion7(),
                preview.OrganizationId,
                preview.PropertyId,
                preview.FileName,
                preview.TotalRows,
                created,
                updated,
                0,
                command.Actor.UserId,
                command.Actor.OccurredAtUtc));
            await store.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            previewStore.Remove(command.PreviewToken);

            return new PersonnelImportConfirmResult(created, updated, 0, results);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return WorkforceError.PersonnelImportFailed("Import could not be completed.");
        }
    }

    private async Task<WorkforceError?> ValidateConfirmedRowAsync(
        PersonnelImportPreviewStore.ValidatedImportRow row,
        PersonnelImportContext context,
        CancellationToken cancellationToken)
    {
        if (row.Action == PersonnelImportAction.Create && row.CreateCommand is not null)
        {
            var department = context.Departments.FirstOrDefault(item => item.Id == row.CreateCommand.DepartmentId);
            var position = context.Positions.FirstOrDefault(item => item.Id == row.CreateCommand.PositionId);
            if (department is null || position is null)
            {
                return WorkforceError.PersonnelImportFailed("Department or position is no longer valid.");
            }

            if (!context.Applicabilities.Any(item =>
                    item.DepartmentId == department.Id && item.PositionId == position.Id))
            {
                return WorkforceError.PersonnelImportFailed(
                    $"Position \"{position.Name}\" is not available for department \"{department.Name}\".");
            }

            return null;
        }

        if (row.Action == PersonnelImportAction.Update
            && row.UpdateCommand is not null
            && row.ExistingEmployeeId is Guid employeeId)
        {
            var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
            if (employee is null || employee.OrganizationId != context.OrganizationId)
            {
                return WorkforceError.PersonnelImportFailed("Employee is no longer available for update.");
            }

            var employment = await store.ListEmploymentsAsync(employeeId, cancellationToken);
            var activeAssignment = await ResolveActiveAssignmentAsync(employment, cancellationToken);
            if (activeAssignment is null)
            {
                return WorkforceError.PersonnelImportFailed("Employee has no active assignment.");
            }

            var department = context.Departments.FirstOrDefault(item => item.Id == activeAssignment.DepartmentId);
            var position = context.Positions.FirstOrDefault(item => item.Id == activeAssignment.PositionId);
            if (department is null || position is null)
            {
                return WorkforceError.PersonnelImportFailed("Employee assignment is no longer valid for this property.");
            }

            if (!context.Applicabilities.Any(item =>
                    item.DepartmentId == department.Id && item.PositionId == position.Id))
            {
                return WorkforceError.PersonnelImportFailed(
                    $"Position \"{position.Name}\" is not available for department \"{department.Name}\".");
            }
        }

        return null;
    }

    private async Task<Assignment?> ResolveActiveAssignmentAsync(
        IReadOnlyList<Employment> employments,
        CancellationToken cancellationToken)
    {
        foreach (var employment in employments.OrderByDescending(item => item.Period.Start))
        {
            var assignments = await store.ListAssignmentsAsync(employment.Id, cancellationToken);
            var active = assignments.FirstOrDefault(item => item.Period.End is null);
            if (active is not null)
            {
                return active;
            }
        }

        return null;
    }

    private async Task<PersonnelImportContext> BuildImportContextAsync(
        Guid organizationId,
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var departments = await store.ListDepartmentsAsync(propertyId, cancellationToken);
        var positions = await store.ListPositionsAsync(propertyId, cancellationToken);
        var applicabilities = await store.ListApplicabilitiesForPositionsAsync(
            positions.Select(item => item.Id).ToArray(),
            cancellationToken);
        return new PersonnelImportContext(organizationId, propertyId, departments, positions, applicabilities);
    }

    private async Task<PersonnelImportRowPreview> ValidateRowAsync(
        PersonnelImportRawRow raw,
        PersonnelImportContext context,
        bool canWriteSensitive,
        Dictionary<string, int> personnelNumbersInFile,
        CancellationToken cancellationToken)
    {
        var errors = new List<PersonnelImportRowError>();
        var cells = raw.Cells;

        var personnelNumber = GetCell(cells, PersonnelImportTemplateColumns.PersonnelNumber);
        var givenName = GetCell(cells, PersonnelImportTemplateColumns.GivenName);
        var familyName = GetCell(cells, PersonnelImportTemplateColumns.FamilyName);
        var departmentCode = GetCell(cells, PersonnelImportTemplateColumns.DepartmentCode);
        var departmentName = GetCell(cells, PersonnelImportTemplateColumns.Department);
        var positionCode = GetCell(cells, PersonnelImportTemplateColumns.PositionCode);
        var positionName = GetCell(cells, PersonnelImportTemplateColumns.Position);
        var startDateRaw = GetCell(cells, PersonnelImportTemplateColumns.EmploymentStartDate);

        if (string.IsNullOrWhiteSpace(givenName))
        {
            errors.Add(new("givenName", "required", "Given name is required."));
        }

        if (string.IsNullOrWhiteSpace(familyName))
        {
            errors.Add(new("familyName", "required", "Family name is required."));
        }

        if (!TryParseDate(startDateRaw, out var startDate))
        {
            errors.Add(new("employmentStartDate", "invalid", "Employment start date is invalid."));
            startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        var department = ResolveDepartment(context, departmentCode, departmentName, errors);
        var position = ResolvePosition(context, positionCode, positionName, errors);
        if (department is not null && position is not null
            && !context.Applicabilities.Any(item =>
                item.DepartmentId == department.Id && item.PositionId == position.Id))
        {
            errors.Add(new(
                "position",
                "position-not-available-for-department",
                $"Position \"{position.Name}\" is not available for department \"{department.Name}\"."));
        }

        PersonnelImportAction action = PersonnelImportAction.Create;
        Employee? existing = null;
        if (!string.IsNullOrWhiteSpace(personnelNumber))
        {
            existing = await store.FindEmployeeByPersonnelNumberAsync(
                context.OrganizationId,
                personnelNumber.Trim(),
                cancellationToken);
            if (existing is null)
            {
                errors.Add(new(
                    "personnelNumber",
                    "personnel-import-duplicate-personnel-number",
                    "Personnel number was not found for update."));
            }
            else
            {
                action = PersonnelImportAction.Update;
            }

            if (personnelNumbersInFile.TryGetValue(personnelNumber.Trim(), out var otherRow))
            {
                errors.Add(new(
                    "personnelNumber",
                    "personnel-import-duplicate-personnel-number",
                    $"Duplicate personnel number in row {otherRow}."));
            }
            else
            {
                personnelNumbersInFile[personnelNumber.Trim()] = raw.RowNumber;
            }
        }

        ValidateImportedFields(cells, canWriteSensitive, errors);

        return new PersonnelImportRowPreview(
            raw.RowNumber,
            action,
            personnelNumber,
            givenName ?? string.Empty,
            familyName ?? string.Empty,
            department?.Name ?? departmentName ?? string.Empty,
            position?.Name ?? positionName ?? string.Empty,
            startDate,
            [],
            errors);
    }

    private async Task<PersonnelImportPreviewStore.ValidatedImportRow?> BuildValidatedRowAsync(
        PersonnelImportRowPreview preview,
        PersonnelImportRawRow raw,
        PersonnelImportContext context,
        bool canWriteSensitive,
        CancellationToken cancellationToken)
    {
        var cells = raw.Cells;
        var department = ResolveDepartment(
            context,
            GetCell(cells, PersonnelImportTemplateColumns.DepartmentCode),
            GetCell(cells, PersonnelImportTemplateColumns.Department),
            [])!;
        var position = ResolvePosition(
            context,
            GetCell(cells, PersonnelImportTemplateColumns.PositionCode),
            GetCell(cells, PersonnelImportTemplateColumns.Position),
            [])!;
        TryParseDate(GetCell(cells, PersonnelImportTemplateColumns.EmploymentStartDate), out var startDate);

        var profile = BuildProfileWriteModel(cells, canWriteSensitive);
        var paymentIban = NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.PaymentIban));
        var paymentBank = NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.PaymentBankName));

        if (preview.Action == PersonnelImportAction.Create)
        {
            return new PersonnelImportPreviewStore.ValidatedImportRow(
                preview.RowNumber,
                preview.Action,
                null,
                new HireEmployeeWithProfileCommand(
                    preview.GivenName,
                    preview.FamilyName,
                    startDate,
                    department.Id,
                    position.Id,
                    profile,
                    canWriteSensitive),
                null,
                [],
                paymentIban,
                paymentBank);
        }

        var personnelNumber = GetCell(cells, PersonnelImportTemplateColumns.PersonnelNumber)!;
        var existing = await store.FindEmployeeByPersonnelNumberAsync(
            context.OrganizationId,
            personnelNumber.Trim(),
            cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var existingProfile = await store.GetHrProfileAsync(existing.Id, cancellationToken);
        var existingContacts = canWriteSensitive
            ? await store.ListEmergencyContactsAsync(existing.Id, cancellationToken)
            : [];
        var mergedProfile = MergeProfileForUpdate(existingProfile, profile, existingContacts, canWriteSensitive);
        var changedFields = ComputeChangedFields(existing, existingProfile, mergedProfile, preview);

        return new PersonnelImportPreviewStore.ValidatedImportRow(
            preview.RowNumber,
            preview.Action,
            existing.Id,
            null,
            new UpdateEmployeeHrProfileCommand(
                existing.Id,
                preview.GivenName,
                preview.FamilyName,
                mergedProfile,
                canWriteSensitive),
            changedFields,
            paymentIban,
            paymentBank);
    }

    private static HrProfileWriteModel BuildProfileWriteModel(
        IReadOnlyDictionary<string, string> cells,
        bool canWriteSensitive)
    {
        PersonnelImportValueParser.TryParseEnum<EducationLevel>(GetCell(cells, PersonnelImportColumnCatalog.Ids.EducationLevel), out var education, out _);
        PersonnelImportValueParser.TryParseEnum<BloodType>(GetCell(cells, PersonnelImportColumnCatalog.Ids.BloodType), out var blood, out _);
        PersonnelImportValueParser.TryParseEnum<Gender>(GetCell(cells, PersonnelImportColumnCatalog.Ids.Gender), out var gender, out _);
        PersonnelImportValueParser.TryParseEnum<MaritalStatus>(GetCell(cells, PersonnelImportColumnCatalog.Ids.MaritalStatus), out var marital, out _);
        PersonnelImportValueParser.TryParseEnum<ForeignLanguageSummary>(GetCell(cells, PersonnelImportColumnCatalog.Ids.ForeignLanguage), out var language, out _);
        PersonnelImportValueParser.TryParseEnum<DrivingLicenceCategory>(GetCell(cells, PersonnelImportColumnCatalog.Ids.DrivingLicenceCategory), out var licence, out _);
        PersonnelImportValueParser.TryParseEnum<MilitaryServiceStatus>(GetCell(cells, PersonnelImportColumnCatalog.Ids.MilitaryServiceStatus), out var military, out _);
        PersonnelImportValueParser.TryParseOptionalDate(GetCell(cells, PersonnelImportColumnCatalog.Ids.BirthDate), out var birthDate, out _);
        PersonnelImportValueParser.TryParseOptionalDate(GetCell(cells, PersonnelImportColumnCatalog.Ids.GraduationDate), out var graduationDate, out _);

        NationalIdentityScheme? scheme = null;
        string? identityNumber = null;
        if (canWriteSensitive)
        {
            PersonnelImportValueParser.TryParseEnum<NationalIdentityScheme>(
                GetCell(cells, PersonnelImportColumnCatalog.Ids.NationalIdentityScheme),
                out scheme,
                out _);
            identityNumber = NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.NationalIdentityNumber));
        }

        return new HrProfileWriteModel(
            scheme,
            identityNumber,
            NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.Nationality)),
            gender,
            birthDate,
            NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.BirthPlace)),
            marital,
            blood,
            education,
            NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.MobilePhone)),
            NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.HomePhone)),
            NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.Email)),
            canWriteSensitive ? NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.ResidenceAddress)) : null,
            canWriteSensitive ? NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.ResidenceCity)) : null,
            canWriteSensitive ? NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.ResidenceDistrict)) : null,
            canWriteSensitive ? NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.NotificationAddress)) : null,
            NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.HrNotes)),
            licence,
            military,
            NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.MilitaryExemptionReason)),
            NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.MilitaryDefermentReason)),
            NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.KepAddress)),
            NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.EducationDescription)),
            NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.SchoolName)),
            graduationDate,
            language,
            BuildEmergencyDrafts(cells, canWriteSensitive));
    }

    private static IReadOnlyList<EmergencyContactDraft> BuildEmergencyDrafts(
        IReadOnlyDictionary<string, string> cells,
        bool canWriteSensitive)
    {
        if (!canWriteSensitive)
        {
            return [];
        }

        var name = NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.EmergencyName));
        var relationship = NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.EmergencyRelationship));
        var phone = NullIfEmpty(GetCell(cells, PersonnelImportColumnCatalog.Ids.EmergencyPhone));
        if (name is null && relationship is null && phone is null)
        {
            return [];
        }

        return [new EmergencyContactDraft(Guid.Empty, name, relationship, phone, true)];
    }

    private static HrProfileWriteModel MergeProfileForUpdate(
        EmployeeHrProfile? existing,
        HrProfileWriteModel incoming,
        IReadOnlyList<EmergencyContact> existingContacts,
        bool canWriteSensitive)
    {
        static string? Pick(string? incomingValue, string? current) =>
            string.IsNullOrWhiteSpace(incomingValue) ? current : incomingValue;

        var emergency = incoming.EmergencyContacts.Count > 0
            ? incoming.EmergencyContacts
            : existingContacts.Select(item => new EmergencyContactDraft(
                item.Id,
                item.Name,
                item.Relationship,
                item.Phone,
                item.IsPrimary)).ToArray();

        return new HrProfileWriteModel(
            canWriteSensitive ? incoming.NationalIdentityScheme ?? existing?.NationalIdentityScheme : existing?.NationalIdentityScheme,
            canWriteSensitive ? Pick(incoming.NationalIdentityNumber, existing?.NationalIdentityNumber) : existing?.NationalIdentityNumber,
            Pick(incoming.Nationality, existing?.Nationality),
            incoming.Gender ?? existing?.Gender,
            incoming.BirthDate ?? existing?.BirthDate,
            Pick(incoming.BirthPlace, existing?.BirthPlace),
            incoming.MaritalStatus ?? existing?.MaritalStatus,
            incoming.BloodType ?? existing?.BloodType,
            incoming.EducationLevel ?? existing?.EducationLevel,
            Pick(incoming.MobilePhone, existing?.MobilePhone),
            Pick(incoming.HomePhone, existing?.HomePhone),
            Pick(incoming.Email, existing?.Email),
            canWriteSensitive ? Pick(incoming.ResidenceAddress, existing?.ResidenceAddress) : existing?.ResidenceAddress,
            canWriteSensitive ? Pick(incoming.ResidenceCity, existing?.ResidenceCity) : existing?.ResidenceCity,
            canWriteSensitive ? Pick(incoming.ResidenceDistrict, existing?.ResidenceDistrict) : existing?.ResidenceDistrict,
            canWriteSensitive ? Pick(incoming.NotificationAddress, existing?.NotificationAddress) : existing?.NotificationAddress,
            Pick(incoming.HrNotes, existing?.HrNotes),
            incoming.DrivingLicenceCategory ?? existing?.DrivingLicenceCategory,
            incoming.MilitaryServiceStatus ?? existing?.MilitaryServiceStatus,
            Pick(incoming.MilitaryExemptionReason, existing?.MilitaryExemptionReason),
            Pick(incoming.MilitaryDefermentReason, existing?.MilitaryDefermentReason),
            Pick(incoming.KepAddress, existing?.KepAddress),
            Pick(incoming.EducationDescription, existing?.EducationDescription),
            Pick(incoming.SchoolName, existing?.SchoolName),
            incoming.GraduationDate ?? existing?.GraduationDate,
            incoming.ForeignLanguage ?? existing?.ForeignLanguage,
            emergency);
    }


    private static IReadOnlyList<string> ComputeChangedFields(
        Employee employee,
        EmployeeHrProfile? profile,
        HrProfileWriteModel merged,
        PersonnelImportRowPreview preview)
    {
        var fields = new List<string>();
        if (!string.Equals(employee.GivenName, preview.GivenName, StringComparison.Ordinal))
        {
            fields.Add("givenName");
        }

        if (!string.Equals(employee.FamilyName, preview.FamilyName, StringComparison.Ordinal))
        {
            fields.Add("familyName");
        }

        if (!string.Equals(profile?.MobilePhone, merged.MobilePhone, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(merged.MobilePhone))
        {
            fields.Add("mobilePhone");
        }

        if (!string.Equals(profile?.Email, merged.Email, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(merged.Email))
        {
            fields.Add("email");
        }

        return fields;
    }

    private static void ValidateImportedFields(
        IReadOnlyDictionary<string, string> cells,
        bool canWriteSensitive,
        List<PersonnelImportRowError> errors)
    {
        ValidatePhone(cells, PersonnelImportColumnCatalog.Ids.MobilePhone, "mobilePhone", "Mobile phone is invalid.", errors);
        ValidatePhone(cells, PersonnelImportColumnCatalog.Ids.HomePhone, "homePhone", "Home phone is invalid.", errors);

        var email = GetCell(cells, PersonnelImportColumnCatalog.Ids.Email);
        if (!string.IsNullOrWhiteSpace(email) && !ContactValue.TryNormalizeEmail(email, out _, out _))
        {
            errors.Add(new("email", HrValidation.Codes.EmailInvalid, "Email is invalid."));
        }

        var kep = GetCell(cells, PersonnelImportColumnCatalog.Ids.KepAddress);
        if (!string.IsNullOrWhiteSpace(kep) && !ContactValue.TryNormalizeEmail(kep, out _, out _))
        {
            errors.Add(new("kepAddress", HrValidation.Codes.KepInvalid, "KEP address is invalid."));
        }

        var nationality = GetCell(cells, PersonnelImportColumnCatalog.Ids.Nationality);
        if (!string.IsNullOrWhiteSpace(nationality)
            && !Iso3166Alpha2Catalog.TryNormalize(nationality, out _, out _))
        {
            errors.Add(new("nationality", HrValidation.Codes.InvalidNationality, "Nationality is invalid."));
        }

        ValidateEnum<EducationLevel>(cells, PersonnelImportColumnCatalog.Ids.EducationLevel, "educationLevel", "Education level is invalid.", errors);
        ValidateEnum<BloodType>(cells, PersonnelImportColumnCatalog.Ids.BloodType, "bloodType", "Blood type is invalid.", errors);
        ValidateEnum<Gender>(cells, PersonnelImportColumnCatalog.Ids.Gender, "gender", "Gender is invalid.", errors);
        ValidateEnum<MaritalStatus>(cells, PersonnelImportColumnCatalog.Ids.MaritalStatus, "maritalStatus", "Marital status is invalid.", errors);
        ValidateEnum<ForeignLanguageSummary>(cells, PersonnelImportColumnCatalog.Ids.ForeignLanguage, "foreignLanguage", "Foreign language is invalid.", errors);
        ValidateEnum<DrivingLicenceCategory>(cells, PersonnelImportColumnCatalog.Ids.DrivingLicenceCategory, "drivingLicenceCategory", "Driving licence is invalid.", errors);
        ValidateEnum<MilitaryServiceStatus>(cells, PersonnelImportColumnCatalog.Ids.MilitaryServiceStatus, "militaryServiceStatus", "Military status is invalid.", errors);
        ValidateOptionalDate(cells, PersonnelImportColumnCatalog.Ids.BirthDate, "birthDate", "Birth date is invalid.", errors);
        ValidateOptionalDate(cells, PersonnelImportColumnCatalog.Ids.GraduationDate, "graduationDate", "Graduation date is invalid.", errors);

        PersonnelImportValueParser.TryParseEnum<MilitaryServiceStatus>(
            GetCell(cells, PersonnelImportColumnCatalog.Ids.MilitaryServiceStatus),
            out var military,
            out _);
        var exemption = GetCell(cells, PersonnelImportColumnCatalog.Ids.MilitaryExemptionReason);
        var deferment = GetCell(cells, PersonnelImportColumnCatalog.Ids.MilitaryDefermentReason);
        if (military == MilitaryServiceStatus.Exempt && string.IsNullOrWhiteSpace(exemption))
        {
            errors.Add(new("militaryExemptionReason", HrValidation.Codes.MilitaryExemptionReasonRequired, "Military exemption reason is required."));
        }

        if (military == MilitaryServiceStatus.Deferred && string.IsNullOrWhiteSpace(deferment))
        {
            errors.Add(new("militaryDefermentReason", HrValidation.Codes.MilitaryDefermentReasonRequired, "Military deferment reason is required."));
        }

        if (military is not MilitaryServiceStatus.Exempt && !string.IsNullOrWhiteSpace(exemption))
        {
            errors.Add(new("militaryExemptionReason", "invalid", "Exemption reason is only valid when military status is Exempt."));
        }

        if (military is not MilitaryServiceStatus.Deferred && !string.IsNullOrWhiteSpace(deferment))
        {
            errors.Add(new("militaryDefermentReason", "invalid", "Deferment reason is only valid when military status is Deferred."));
        }

        var notes = GetCell(cells, PersonnelImportColumnCatalog.Ids.HrNotes);
        if (!string.IsNullOrWhiteSpace(notes) && notes.Length > ContactValue.NotesMaxLength)
        {
            errors.Add(new("hrNotes", HrValidation.Codes.TextTooLong, "Notes exceed the maximum length."));
        }

        var emergencyName = GetCell(cells, PersonnelImportColumnCatalog.Ids.EmergencyName);
        var emergencyRelationship = GetCell(cells, PersonnelImportColumnCatalog.Ids.EmergencyRelationship);
        var emergencyPhone = GetCell(cells, PersonnelImportColumnCatalog.Ids.EmergencyPhone);
        var emergencyFilled = !string.IsNullOrWhiteSpace(emergencyName)
            || !string.IsNullOrWhiteSpace(emergencyRelationship)
            || !string.IsNullOrWhiteSpace(emergencyPhone);
        if (emergencyFilled && (string.IsNullOrWhiteSpace(emergencyName) || string.IsNullOrWhiteSpace(emergencyPhone)))
        {
            errors.Add(new("emergencyName", "required", "Primary emergency contact requires both name and phone."));
        }

        if (!string.IsNullOrWhiteSpace(emergencyPhone)
            && !ContactValue.TryNormalizePhone(emergencyPhone, required: true, out _, out _))
        {
            errors.Add(new("emergencyPhone", HrValidation.Codes.PhoneInvalid, "Emergency phone is invalid."));
        }

        var iban = GetCell(cells, PersonnelImportColumnCatalog.Ids.PaymentIban);
        var bank = GetCell(cells, PersonnelImportColumnCatalog.Ids.PaymentBankName);
        if (string.IsNullOrWhiteSpace(iban) && !string.IsNullOrWhiteSpace(bank))
        {
            errors.Add(new("paymentIban", "required", "IBAN is required when bank name is provided."));
        }

        if (!string.IsNullOrWhiteSpace(iban) && !PaymentIban.TryNormalize(iban, out _, out _))
        {
            errors.Add(new("paymentIban", "payment-profile-invalid-iban", "IBAN is invalid."));
        }

        var sensitiveFilled = PersonnelImportColumnCatalog.Columns.Any(column =>
            column.Sensitive && !string.IsNullOrWhiteSpace(GetCell(cells, column.Id)));
        if (sensitiveFilled && !canWriteSensitive)
        {
            errors.Add(new("sensitive", "sensitive-write-forbidden", "Sensitive fields require sensitive personnel permission."));
        }

        if (canWriteSensitive)
        {
            PersonnelImportValueParser.TryParseEnum<NationalIdentityScheme>(
                GetCell(cells, PersonnelImportColumnCatalog.Ids.NationalIdentityScheme),
                out var scheme,
                out var schemeInvalid);
            if (schemeInvalid)
            {
                errors.Add(new("nationalIdentityScheme", "invalid", "Identity type is invalid."));
            }

            var identityNumber = GetCell(cells, PersonnelImportColumnCatalog.Ids.NationalIdentityNumber);
            if (!NationalIdentity.TryNormalize(scheme, identityNumber, out _, out _, out _, out var identityError)
                && identityError is not null)
            {
                errors.Add(new("nationalIdentityNumber", identityError, "Identity number is invalid."));
            }
        }
    }

    private static void ValidatePhone(
        IReadOnlyDictionary<string, string> cells,
        string columnId,
        string field,
        string message,
        List<PersonnelImportRowError> errors)
    {
        var value = GetCell(cells, columnId);
        if (!string.IsNullOrWhiteSpace(value) && !ContactValue.TryNormalizePhone(value, required: false, out _, out _))
        {
            errors.Add(new(field, HrValidation.Codes.PhoneInvalid, message));
        }
    }

    private static void ValidateEnum<TEnum>(
        IReadOnlyDictionary<string, string> cells,
        string columnId,
        string field,
        string message,
        List<PersonnelImportRowError> errors)
        where TEnum : struct, Enum
    {
        if (!PersonnelImportValueParser.TryParseEnum<TEnum>(GetCell(cells, columnId), out _, out var invalid) && invalid)
        {
            errors.Add(new(field, "invalid", message));
        }
    }

    private static void ValidateOptionalDate(
        IReadOnlyDictionary<string, string> cells,
        string columnId,
        string field,
        string message,
        List<PersonnelImportRowError> errors)
    {
        if (!PersonnelImportValueParser.TryParseOptionalDate(GetCell(cells, columnId), out _, out var invalid) && invalid)
        {
            errors.Add(new(field, "invalid", message));
        }
    }

    private async Task<WorkforceError?> TrySavePaymentAsync(
        Guid employeeId,
        PersonnelImportPreviewStore.ValidatedImportRow row,
        PersonnelImportConfirmCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(row.PaymentIban))
        {
            return null;
        }

        var saved = await payment.ExecuteAsync(
            new SaveEmployeePaymentProfileCommand(
                employeeId,
                row.PaymentIban,
                row.PaymentBankName,
                command.CanWriteSensitive,
                command.Actor),
            cancellationToken);
        return saved.IsSuccess ? null : WorkforceError.PersonnelImportFailed(saved.Error!.Detail);
    }

    private static Department? ResolveDepartment(
        PersonnelImportContext context,
        string? code,
        string? name,
        List<PersonnelImportRowError> errors)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            var matches = context.Departments
                .Where(item => string.Equals(item.Code, code.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length == 1)
            {
                return matches[0];
            }

            if (matches.Length == 0)
            {
                errors.Add(new("department", "personnel-import-department-not-found", "Department was not found."));
                return null;
            }

            errors.Add(new("department", "personnel-import-department-not-found", "Department code is ambiguous."));
            return null;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(new("department", "required", "Department is required."));
            return null;
        }

        var byName = context.Departments
            .Where(item => string.Equals(item.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (byName.Length == 1)
        {
            return byName[0];
        }

        errors.Add(new(
            "department",
            byName.Length == 0 ? "personnel-import-department-not-found" : "personnel-import-department-not-found",
            byName.Length == 0 ? "Department was not found." : "Department name is not unique."));
        return null;
    }

    private static Position? ResolvePosition(
        PersonnelImportContext context,
        string? code,
        string? name,
        List<PersonnelImportRowError> errors)
    {
        if (!string.IsNullOrWhiteSpace(code))
        {
            var matches = context.Positions
                .Where(item => string.Equals(item.Code, code.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length == 1)
            {
                return matches[0];
            }

            errors.Add(new("position", "personnel-import-position-not-found", "Position was not found."));
            return null;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(new("position", "required", "Position is required."));
            return null;
        }

        var byName = context.Positions
            .Where(item => string.Equals(item.Name, name.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (byName.Length == 1)
        {
            return byName[0];
        }

        errors.Add(new(
            "position",
            "personnel-import-position-not-found",
            byName.Length == 0 ? "Position was not found." : "Position name is not unique."));
        return null;
    }

    private static string? GetCell(IReadOnlyDictionary<string, string> cells, string columnId) =>
        cells.TryGetValue(columnId, out var value)
            ? SpreadsheetSafety.SanitizeImportedCell(value)
            : null;

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryParseDate(string? raw, out DateOnly date) =>
        PersonnelImportValueParser.TryParseDate(raw, out date);

    private sealed record PersonnelImportContext(
        Guid OrganizationId,
        Guid PropertyId,
        IReadOnlyList<Department> Departments,
        IReadOnlyList<Position> Positions,
        IReadOnlyList<DepartmentPositionApplicability> Applicabilities);
}

public sealed record PersonnelImportPreviewCommand(
    Stream Content,
    long ContentLength,
    string FileName,
    bool CanWriteSensitive,
    string ActorUserId);
