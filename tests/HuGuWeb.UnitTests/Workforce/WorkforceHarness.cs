using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

internal sealed class FakeClock : IWorkforceClock
{
    public DateOnly Today { get; set; } = new(2026, 8, 21);
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 21, 8, 0, 0, TimeSpan.Zero);
}

internal sealed class FixedWorkplace(Guid organizationId, Guid propertyId) : IWorkplaceContext
{
    public Guid OrganizationId { get; } = organizationId;
    public Guid PropertyId { get; } = propertyId;
    public bool HasOrganization => OrganizationId != Guid.Empty;
    public bool HasProperty => PropertyId != Guid.Empty;
    public bool IsConfigured => HasOrganization;
}

internal sealed class InMemoryWorkforceStore : IWorkforceStore
{
    private InMemoryWorkforceSnapshot? _transactionSnapshot;
    private int _transactionSaveCount;

    public int? FailSaveChangesAfterCount { get; set; }

    public List<Organization> Organizations { get; } = [];
    public List<Property> Properties { get; } = [];
    public List<Department> Departments { get; } = [];
    public List<Position> Positions { get; } = [];
    public List<DepartmentPositionApplicability> Applicabilities { get; } = [];
    public Dictionary<Guid, PersonnelNumberSequence> Sequences { get; } = [];
    public List<Employee> Employees { get; } = [];
    public List<Employment> Employments { get; } = [];
    public List<Assignment> Assignments { get; } = [];
    public List<PersonnelMovement> PersonnelMovements { get; } = [];
    public List<WorkforceReportingLine> ReportingLines { get; } = [];
    public List<EmployeeHrProfile> HrProfiles { get; } = [];
    public List<EmergencyContact> EmergencyContacts { get; } = [];
    public List<EmployeeCertificate> EmployeeCertificates { get; } = [];
    public List<EmployeePhoto> Photos { get; } = [];
    public List<SgkWorkplaceRegistration> SgkWorkplaceRegistrations { get; } = [];
    public List<OfficialEmploymentProfile> OfficialEmploymentProfiles { get; } = [];
    public List<SgkDocumentType> SgkDocumentTypes { get; } = [];
    public List<ApplicableLawCode> ApplicableLawCodes { get; } = [];
    public List<InsuranceBranch> InsuranceBranches { get; } = [];
    public List<SgkOccupationCode> SgkOccupationCodes { get; } = [];
    public List<EmploymentDutyCode> EmploymentDutyCodes { get; } = [];
    public List<EmploymentBesSettings> EmploymentBesSettings { get; } = [];
    public List<EmployeePaymentProfile> PaymentProfiles { get; } = [];
    public List<PersonnelProfileChange> ProfileChanges { get; } = [];
    public List<PersonnelImportRun> ImportRuns { get; } = [];
    public List<LeaveType> LeaveTypes { get; } = [];
    public List<LeaveEntitlement> LeaveEntitlements { get; } = [];
    public List<LeaveRecord> LeaveRecords { get; } = [];
    public List<LeaveRequest> LeaveRequests { get; } = [];
    public List<LeaveRequestDecision> LeaveRequestDecisions { get; } = [];
    public List<ShiftDefinition> ShiftDefinitions { get; } = [];
    public List<ScheduleEntry> ScheduleEntries { get; } = [];
    public List<ScheduleEntryChange> ScheduleEntryChanges { get; } = [];
    public List<AttendanceCorrection> AttendanceCorrections { get; } = [];
    public List<AttendanceCorrectionChange> AttendanceCorrectionChanges { get; } = [];
    public List<RecruitmentSource> RecruitmentSources { get; } = [];
    public List<OnboardingDocumentRequirement> OnboardingDocumentRequirements { get; } = [];
    public List<EmploymentOnboardingDocumentStatus> EmploymentOnboardingDocumentStatuses { get; } = [];
    public List<HrDocumentTemplate> HrDocumentTemplates { get; } = [];

    public Task<Organization?> GetOrganizationAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Organizations.FirstOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<Guid>> ListOrganizationIdsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Guid>>(Organizations.Select(item => item.Id).ToArray());

    public Task<Property?> GetPropertyAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Properties.FirstOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<Property>> ListPropertiesAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Property>>(
            Properties.Where(item => item.OrganizationId == organizationId).ToArray());

    public Task<Department?> GetDepartmentAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Departments.FirstOrDefault(item => item.Id == id));

    public Task<Position?> GetPositionAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Positions.FirstOrDefault(item => item.Id == id));

    public Task<Employee?> GetEmployeeAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Employees.FirstOrDefault(item => item.Id == id));

    public Task<Employee?> FindEmployeeByPersonnelNumberAsync(
        Guid organizationId,
        string personnelNumber,
        CancellationToken cancellationToken) =>
        Task.FromResult(Employees.FirstOrDefault(item =>
            item.OrganizationId == organizationId && item.PersonnelNumber == personnelNumber));

    public Task<IReadOnlyList<Department>> ListDepartmentsAsync(Guid propertyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Department>>(Departments.Where(item => item.PropertyId == propertyId).ToArray());

    public Task<IReadOnlyList<Department>> ListDepartmentsForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var propertyIds = Properties.Where(item => item.OrganizationId == organizationId).Select(item => item.Id).ToHashSet();
        return Task.FromResult<IReadOnlyList<Department>>(
            Departments.Where(item => propertyIds.Contains(item.PropertyId)).ToArray());
    }

    public Task<IReadOnlyList<Position>> ListPositionsAsync(
        Guid propertyId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Position>>(
            Positions.Where(item => item.PropertyId == propertyId).ToArray());

    public Task<IReadOnlyList<Position>> ListPositionsForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var propertyIds = Properties.Where(item => item.OrganizationId == organizationId).Select(item => item.Id).ToHashSet();
        return Task.FromResult<IReadOnlyList<Position>>(
            Positions.Where(item => propertyIds.Contains(item.PropertyId)).ToArray());
    }

    public Task<IReadOnlyList<DepartmentPositionApplicability>> ListApplicabilitiesForPositionsAsync(
        IReadOnlyCollection<Guid> positionIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<DepartmentPositionApplicability>>(
            Applicabilities.Where(item => positionIds.Contains(item.PositionId)).ToArray());

    public Task<bool> IsPositionApplicableToDepartmentAsync(
        Guid departmentId,
        Guid positionId,
        CancellationToken cancellationToken) =>
        Task.FromResult(Applicabilities.Any(item =>
            item.DepartmentId == departmentId && item.PositionId == positionId));

    public Task<string> AllocatePersonnelNumberAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        lock (this)
        {
            if (!Sequences.TryGetValue(organizationId, out var sequence))
            {
                sequence = new PersonnelNumberSequence(organizationId, PersonnelNumberSequence.StartingValue);
                Sequences[organizationId] = sequence;
            }

            while (true)
            {
                var formatted = PersonnelNumber.Format(sequence.ReserveNext());
                if (!Employees.Any(item =>
                        item.OrganizationId == organizationId && item.PersonnelNumber == formatted))
                {
                    return Task.FromResult(formatted);
                }
            }
        }
    }

    public Task<IReadOnlyList<Employee>> ListEmployeesAsync(Guid organizationId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Employee>>(Employees.Where(item => item.OrganizationId == organizationId).ToArray());

    public Task<IReadOnlyList<Employment>> ListEmploymentsAsync(Guid employeeId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Employment>>(Employments.Where(item => item.EmployeeId == employeeId).ToArray());

    public Task<Employment?> GetEmploymentAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Employments.FirstOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<Employment>> ListEmploymentsForEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Employment>>(
            Employments.Where(item => employeeIds.Contains(item.EmployeeId)).ToArray());

    public Task<IReadOnlyList<Assignment>> ListAssignmentsAsync(Guid employmentId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Assignment>>(Assignments.Where(item => item.EmploymentId == employmentId).ToArray());

    public Task<IReadOnlyList<Assignment>> ListAssignmentsForEmploymentsAsync(
        IReadOnlyCollection<Guid> employmentIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Assignment>>(
            Assignments.Where(item => employmentIds.Contains(item.EmploymentId)).ToArray());

    public Task<Assignment?> GetAssignmentAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Assignments.FirstOrDefault(item => item.Id == id));

    public void AddDepartment(Department department) => Departments.Add(department);

    public void AddPosition(Position position) => Positions.Add(position);

    public void AddApplicability(DepartmentPositionApplicability applicability) =>
        Applicabilities.Add(applicability);

    public void RemoveApplicability(DepartmentPositionApplicability applicability) =>
        Applicabilities.Remove(applicability);

    public void AddEmployee(Employee employee) => Employees.Add(employee);

    public void AddEmployment(Employment employment) => Employments.Add(employment);

    public void AddAssignment(Assignment assignment) => Assignments.Add(assignment);

    public void RemoveAssignment(Assignment assignment) => Assignments.Remove(assignment);

    public Task<PersonnelMovement?> GetPersonnelMovementAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(PersonnelMovements.FirstOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<PersonnelMovement>> ListPersonnelMovementsAsync(
        Guid organizationId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        PersonnelMovementType? type,
        IReadOnlyCollection<Guid>? employmentIds,
        CancellationToken cancellationToken)
    {
        IEnumerable<PersonnelMovement> query = PersonnelMovements.Where(item => item.OrganizationId == organizationId);
        if (dateFrom is { } from)
        {
            query = query.Where(item => item.EffectiveDate >= from);
        }

        if (dateTo is { } to)
        {
            query = query.Where(item => item.EffectiveDate <= to);
        }

        if (type is { } movementType)
        {
            query = query.Where(item => item.MovementType == movementType);
        }

        if (employmentIds is not null)
        {
            query = query.Where(item => employmentIds.Contains(item.EmploymentId));
        }

        return Task.FromResult<IReadOnlyList<PersonnelMovement>>(
            query
                .OrderByDescending(item => item.EffectiveDate)
                .ThenByDescending(item => item.CreatedAtUtc)
                .ToArray());
    }

    public void AddPersonnelMovement(PersonnelMovement movement) => PersonnelMovements.Add(movement);

    public Task<WorkforceReportingLine?> GetReportingLineAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(ReportingLines.FirstOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<WorkforceReportingLine>> ListReportingLinesForEmploymentAsync(
        Guid subordinateEmploymentId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<WorkforceReportingLine>>(
            ReportingLines
                .Where(item => item.SubordinateEmploymentId == subordinateEmploymentId)
                .OrderBy(item => item.EffectiveFrom)
                .ToArray());

    public Task<IReadOnlyList<WorkforceReportingLine>> ListReportingLinesForEmploymentsAsync(
        IReadOnlyCollection<Guid> subordinateEmploymentIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<WorkforceReportingLine>>(
            ReportingLines
                .Where(item => subordinateEmploymentIds.Contains(item.SubordinateEmploymentId))
                .OrderBy(item => item.EffectiveFrom)
                .ToArray());

    public void AddReportingLine(WorkforceReportingLine line) => ReportingLines.Add(line);

    public void RemoveReportingLine(WorkforceReportingLine line) => ReportingLines.Remove(line);

    public Task<EmployeeHrProfile?> GetHrProfileAsync(Guid employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(HrProfiles.FirstOrDefault(item => item.EmployeeId == employeeId));

    public Task<IReadOnlyList<EmployeeHrProfile>> ListHrProfilesForEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EmployeeHrProfile>>(
            HrProfiles.Where(item => employeeIds.Contains(item.EmployeeId)).ToArray());

    public Task<EmployeeHrProfile?> FindHrProfileByNationalIdentityAsync(
        Guid organizationId,
        NationalIdentityScheme scheme,
        string normalizedNationalIdentityNumber,
        CancellationToken cancellationToken) =>
        Task.FromResult(HrProfiles.FirstOrDefault(item =>
            item.OrganizationId == organizationId
            && item.NationalIdentityScheme == scheme
            && item.NormalizedNationalIdentityNumber == normalizedNationalIdentityNumber));

    public Task<IReadOnlyList<EmergencyContact>> ListEmergencyContactsAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EmergencyContact>>(
            EmergencyContacts.Where(item => item.EmployeeId == employeeId).ToArray());

    public Task<IReadOnlyList<EmergencyContact>> ListEmergencyContactsForEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EmergencyContact>>(
            EmergencyContacts.Where(item => employeeIds.Contains(item.EmployeeId)).ToArray());

    public Task<EmployeePhoto?> GetEmployeePhotoAsync(Guid employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(Photos.FirstOrDefault(item => item.EmployeeId == employeeId));

    public Task<IReadOnlyList<EmployeePhoto>> ListEmployeePhotosForEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EmployeePhoto>>(
            Photos.Where(item => employeeIds.Contains(item.EmployeeId)).ToArray());

    public void AddHrProfile(EmployeeHrProfile profile) => HrProfiles.Add(profile);

    public void AddEmergencyContact(EmergencyContact contact) => EmergencyContacts.Add(contact);

    public void RemoveEmergencyContact(EmergencyContact contact) => EmergencyContacts.Remove(contact);

    public void AddEmployeePhoto(EmployeePhoto photo) => Photos.Add(photo);

    public void RemoveEmployeePhoto(EmployeePhoto photo) => Photos.Remove(photo);

    public Task<IReadOnlyList<EmployeeCertificate>> ListEmployeeCertificatesAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EmployeeCertificate>>(
            EmployeeCertificates.Where(item => item.EmployeeId == employeeId)
                .OrderBy(item => item.SortOrder)
                .ToArray());

    public void AddEmployeeCertificate(EmployeeCertificate certificate) => EmployeeCertificates.Add(certificate);

    public void RemoveEmployeeCertificate(EmployeeCertificate certificate) =>
        EmployeeCertificates.Remove(certificate);

    public Task<IReadOnlyList<RecruitmentSource>> ListRecruitmentSourcesAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RecruitmentSource>>(
            RecruitmentSources.Where(item => item.OrganizationId == organizationId)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Name)
                .ToArray());

    public Task<RecruitmentSource?> GetRecruitmentSourceAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(RecruitmentSources.FirstOrDefault(item => item.Id == id));

    public void AddRecruitmentSource(RecruitmentSource source) => RecruitmentSources.Add(source);

    public Task<IReadOnlyList<OnboardingDocumentRequirement>> ListOnboardingDocumentRequirementsAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OnboardingDocumentRequirement>>(
            OnboardingDocumentRequirements.Where(item => item.OrganizationId == organizationId)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Name)
                .ToArray());

    public Task<OnboardingDocumentRequirement?> GetOnboardingDocumentRequirementAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        Task.FromResult(OnboardingDocumentRequirements.FirstOrDefault(item => item.Id == id));

    public void AddOnboardingDocumentRequirement(OnboardingDocumentRequirement requirement) =>
        OnboardingDocumentRequirements.Add(requirement);

    public Task<IReadOnlyList<EmploymentOnboardingDocumentStatus>> ListEmploymentOnboardingDocumentStatusesAsync(
        Guid employmentId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EmploymentOnboardingDocumentStatus>>(
            EmploymentOnboardingDocumentStatuses.Where(item => item.EmploymentId == employmentId).ToArray());

    public void AddEmploymentOnboardingDocumentStatus(EmploymentOnboardingDocumentStatus status) =>
        EmploymentOnboardingDocumentStatuses.Add(status);

    public Task<IReadOnlyList<HrDocumentTemplate>> ListHrDocumentTemplatesAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<HrDocumentTemplate>>(
            HrDocumentTemplates.Where(item => item.OrganizationId == organizationId)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Name)
                .ToArray());

    public Task<HrDocumentTemplate?> GetHrDocumentTemplateAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(HrDocumentTemplates.FirstOrDefault(item => item.Id == id));

    public void AddHrDocumentTemplate(HrDocumentTemplate template) => HrDocumentTemplates.Add(template);

    public Task<SgkWorkplaceRegistration?> GetSgkWorkplaceRegistrationAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        Task.FromResult(SgkWorkplaceRegistrations.FirstOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<SgkWorkplaceRegistration>> ListSgkWorkplaceRegistrationsAsync(
        Guid propertyId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SgkWorkplaceRegistration>>(
            SgkWorkplaceRegistrations.Where(item => item.PropertyId == propertyId).ToArray());

    public void AddSgkWorkplaceRegistration(SgkWorkplaceRegistration registration) =>
        SgkWorkplaceRegistrations.Add(registration);

    public Task<OfficialEmploymentProfile?> GetOfficialEmploymentProfileAsync(
        Guid employmentId,
        CancellationToken cancellationToken) =>
        Task.FromResult(OfficialEmploymentProfiles.FirstOrDefault(item => item.EmploymentId == employmentId));

    public Task<IReadOnlyList<OfficialEmploymentProfile>> ListOfficialEmploymentProfilesForEmploymentsAsync(
        IReadOnlyCollection<Guid> employmentIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OfficialEmploymentProfile>>(
            OfficialEmploymentProfiles.Where(item => employmentIds.Contains(item.EmploymentId)).ToArray());

    public void AddOfficialEmploymentProfile(OfficialEmploymentProfile profile) =>
        OfficialEmploymentProfiles.Add(profile);

    public Task<SgkDocumentType?> GetSgkDocumentTypeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(SgkDocumentTypes.FirstOrDefault(item => item.Code == code));

    public Task<IReadOnlyList<SgkDocumentType>> ListSgkDocumentTypesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SgkDocumentType>>(SgkDocumentTypes.ToArray());

    public Task<ApplicableLawCode?> GetApplicableLawCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(ApplicableLawCodes.FirstOrDefault(item => item.Code == code));

    public Task<IReadOnlyList<ApplicableLawCode>> ListApplicableLawCodesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ApplicableLawCode>>(ApplicableLawCodes.ToArray());

    public Task<InsuranceBranch?> GetInsuranceBranchAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(InsuranceBranches.FirstOrDefault(item => item.Code == code));

    public Task<IReadOnlyList<InsuranceBranch>> ListInsuranceBranchesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<InsuranceBranch>>(InsuranceBranches.ToArray());

    public Task<SgkOccupationCode?> GetSgkOccupationCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(SgkOccupationCodes.FirstOrDefault(item => item.Code == code));

    public Task<IReadOnlyList<SgkOccupationCode>> SearchSgkOccupationCodesAsync(
        string? query,
        int take,
        CancellationToken cancellationToken)
    {
        IEnumerable<SgkOccupationCode> rows = SgkOccupationCodes.Where(item => item.IsActive);
        var term = query?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            rows = rows.Where(item =>
                item.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
                || item.Description.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return Task.FromResult<IReadOnlyList<SgkOccupationCode>>(
            rows.OrderBy(item => item.Code).Take(Math.Clamp(take, 1, 50)).ToArray());
    }

    public Task<EmploymentDutyCode?> GetEmploymentDutyCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(EmploymentDutyCodes.FirstOrDefault(item => item.Code == code));

    public Task<IReadOnlyList<EmploymentDutyCode>> ListEmploymentDutyCodesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<EmploymentDutyCode>>(EmploymentDutyCodes.ToArray());

    public Task<EmploymentBesSettings?> GetEmploymentBesSettingsAsync(
        Guid employmentId,
        CancellationToken cancellationToken) =>
        Task.FromResult(EmploymentBesSettings.FirstOrDefault(item => item.EmploymentId == employmentId));

    public void AddEmploymentBesSettings(EmploymentBesSettings settings) =>
        EmploymentBesSettings.Add(settings);

    public Task<EmployeePaymentProfile?> GetPaymentProfileAsync(Guid employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(PaymentProfiles.FirstOrDefault(item => item.EmployeeId == employeeId));

    public void AddPaymentProfile(EmployeePaymentProfile profile) => PaymentProfiles.Add(profile);

    public Task<IReadOnlyList<PersonnelProfileChange>> ListPersonnelProfileChangesAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PersonnelProfileChange>>(
            ProfileChanges.Where(item => item.EmployeeId == employeeId).OrderByDescending(item => item.ChangedAtUtc).ToArray());

    public void AddPersonnelProfileChange(PersonnelProfileChange change) => ProfileChanges.Add(change);

    public void AddPersonnelImportRun(PersonnelImportRun importRun) => ImportRuns.Add(importRun);

    public Task<IReadOnlyList<LeaveType>> ListLeaveTypesAsync(Guid organizationId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LeaveType>>(
            LeaveTypes.Where(item => item.OrganizationId == organizationId).OrderBy(item => item.Name).ToArray());

    public Task<LeaveType?> GetLeaveTypeAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(LeaveTypes.FirstOrDefault(item => item.Id == id));

    public Task<LeaveType?> FindLeaveTypeByCodeAsync(
        Guid organizationId,
        string normalizedCode,
        CancellationToken cancellationToken) =>
        Task.FromResult(LeaveTypes.FirstOrDefault(item =>
            item.OrganizationId == organizationId && item.Code == normalizedCode));

    public Task<bool> LeaveTypeHasUsageAsync(Guid leaveTypeId, CancellationToken cancellationToken) =>
        Task.FromResult(
            LeaveEntitlements.Any(item => item.LeaveTypeId == leaveTypeId)
            || LeaveRecords.Any(item => item.LeaveTypeId == leaveTypeId));

    public void AddLeaveType(LeaveType leaveType) => LeaveTypes.Add(leaveType);

    public Task<IReadOnlyList<LeaveEntitlement>> ListLeaveEntitlementsAsync(
        Guid employmentId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LeaveEntitlement>>(
            LeaveEntitlements
                .Where(item => item.EmploymentId == employmentId)
                .OrderByDescending(item => item.EffectiveDate)
                .ThenByDescending(item => item.CreatedAtUtc)
                .ToArray());

    public Task<IReadOnlyList<LeaveRecord>> ListLeaveRecordsAsync(
        Guid employmentId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LeaveRecord>>(
            LeaveRecords
                .Where(item => item.EmploymentId == employmentId)
                .OrderByDescending(item => item.StartDate)
                .ThenByDescending(item => item.CreatedAtUtc)
                .ToArray());

    public Task<LeaveRecord?> GetLeaveRecordAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(LeaveRecords.FirstOrDefault(item => item.Id == id));

    public Task<LeaveRecord?> FindLeaveRecordBySourceLeaveRequestIdAsync(
        Guid leaveRequestId,
        CancellationToken cancellationToken) =>
        Task.FromResult(LeaveRecords.FirstOrDefault(item => item.SourceLeaveRequestId == leaveRequestId));

    public void AddLeaveEntitlement(LeaveEntitlement entitlement) => LeaveEntitlements.Add(entitlement);

    public void AddLeaveRecord(LeaveRecord record) => LeaveRecords.Add(record);

    public Task<IReadOnlyList<LeaveRequest>> ListLeaveRequestsAsync(
        Guid employmentId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LeaveRequest>>(
            LeaveRequests.Where(item => item.EmploymentId == employmentId).ToArray());

    public Task<IReadOnlyList<LeaveRequest>> ListLeaveRequestsForEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var employmentIds = Employments
            .Where(item => item.EmployeeId == employeeId)
            .Select(item => item.Id)
            .ToHashSet();
        return Task.FromResult<IReadOnlyList<LeaveRequest>>(
            LeaveRequests.Where(item => employmentIds.Contains(item.EmploymentId)).ToArray());
    }

    public Task<IReadOnlyList<LeaveRequest>> ListAllLeaveRequestsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LeaveRequest>>(LeaveRequests.ToArray());

    public Task<LeaveRequest?> GetLeaveRequestAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(LeaveRequests.FirstOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<LeaveRequestDecision>> ListLeaveRequestDecisionsAsync(
        Guid leaveRequestId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<LeaveRequestDecision>>(
            LeaveRequestDecisions
                .Where(item => item.LeaveRequestId == leaveRequestId)
                .OrderBy(item => item.DecisionAtUtc)
                .ToArray());

    public void AddLeaveRequest(LeaveRequest request) => LeaveRequests.Add(request);

    public void AddLeaveRequestDecision(LeaveRequestDecision decision) => LeaveRequestDecisions.Add(decision);

    public Task<IReadOnlyList<ShiftDefinition>> ListShiftDefinitionsAsync(
        Guid propertyId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ShiftDefinition>>(
            ShiftDefinitions.Where(item => item.PropertyId == propertyId).OrderBy(item => item.Name).ToArray());

    public Task<ShiftDefinition?> GetShiftDefinitionAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(ShiftDefinitions.FirstOrDefault(item => item.Id == id));

    public Task<ShiftDefinition?> FindShiftDefinitionByCodeAsync(
        Guid propertyId,
        string normalizedCode,
        CancellationToken cancellationToken) =>
        Task.FromResult(ShiftDefinitions.FirstOrDefault(item =>
            item.PropertyId == propertyId && item.Code == normalizedCode));

    public Task<bool> ShiftDefinitionHasUsageAsync(Guid shiftDefinitionId, CancellationToken cancellationToken) =>
        Task.FromResult(
            ScheduleEntries.Any(item => item.ShiftDefinitionId == shiftDefinitionId)
            || ScheduleEntryChanges.Any(item =>
                item.PreviousShiftDefinitionId == shiftDefinitionId
                || item.NewShiftDefinitionId == shiftDefinitionId));

    public Task<IReadOnlyList<Guid>> ListShiftDefinitionIdsWithUsageAsync(
        IReadOnlyCollection<Guid> shiftDefinitionIds,
        CancellationToken cancellationToken)
    {
        if (shiftDefinitionIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<Guid>>([]);
        }

        var live = ScheduleEntries
            .Where(item => item.ShiftDefinitionId is Guid id && shiftDefinitionIds.Contains(id))
            .Select(item => item.ShiftDefinitionId!.Value);
        var history = ScheduleEntryChanges
            .SelectMany(item => new Guid?[] { item.PreviousShiftDefinitionId, item.NewShiftDefinitionId })
            .Where(id => id is Guid value && shiftDefinitionIds.Contains(value))
            .Select(id => id!.Value);

        return Task.FromResult<IReadOnlyList<Guid>>(live.Concat(history).Distinct().ToArray());
    }

    public void AddShiftDefinition(ShiftDefinition definition) => ShiftDefinitions.Add(definition);

    public Task<ScheduleEntry?> GetScheduleEntryAsync(
        Guid employmentId,
        DateOnly scheduleDate,
        CancellationToken cancellationToken) =>
        Task.FromResult(ScheduleEntries.FirstOrDefault(item =>
            item.EmploymentId == employmentId && item.ScheduleDate == scheduleDate));

    public Task<IReadOnlyList<ScheduleEntry>> ListScheduleEntriesAsync(
        IReadOnlyCollection<Guid> employmentIds,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        if (employmentIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<ScheduleEntry>>([]);
        }

        return Task.FromResult<IReadOnlyList<ScheduleEntry>>(
            ScheduleEntries
                .Where(item =>
                    employmentIds.Contains(item.EmploymentId)
                    && item.ScheduleDate >= from
                    && item.ScheduleDate <= to)
                .OrderBy(item => item.ScheduleDate)
                .ToArray());
    }

    public void AddScheduleEntry(ScheduleEntry entry) => ScheduleEntries.Add(entry);

    public void RemoveScheduleEntry(ScheduleEntry entry) => ScheduleEntries.Remove(entry);

    public void AddScheduleEntryChange(ScheduleEntryChange change) => ScheduleEntryChanges.Add(change);

    public Task<AttendanceCorrection?> GetAttendanceCorrectionAsync(
        Guid employmentId,
        DateOnly localDate,
        CancellationToken cancellationToken) =>
        Task.FromResult(AttendanceCorrections.FirstOrDefault(item =>
            item.EmploymentId == employmentId && item.LocalDate == localDate));

    public Task<IReadOnlyList<AttendanceCorrection>> ListAttendanceCorrectionsAsync(
        IReadOnlyCollection<Guid> employmentIds,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        if (employmentIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<AttendanceCorrection>>([]);
        }

        return Task.FromResult<IReadOnlyList<AttendanceCorrection>>(
            AttendanceCorrections
                .Where(item =>
                    employmentIds.Contains(item.EmploymentId)
                    && item.LocalDate >= from
                    && item.LocalDate <= to)
                .OrderBy(item => item.LocalDate)
                .ToArray());
    }

    public Task<IReadOnlyList<AttendanceCorrectionChange>> ListAttendanceCorrectionChangesAsync(
        Guid employmentId,
        DateOnly localDate,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AttendanceCorrectionChange>>(
            AttendanceCorrectionChanges
                .Where(item => item.EmploymentId == employmentId && item.LocalDate == localDate)
                .OrderBy(item => item.ChangedAtUtc)
                .ThenBy(item => item.Id)
                .ToArray());

    public Task<IReadOnlyList<LeaveRecord>> ListRecordedLeaveRecordsOverlappingAsync(
        IReadOnlyCollection<Guid> employmentIds,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        if (employmentIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<LeaveRecord>>([]);
        }

        return Task.FromResult<IReadOnlyList<LeaveRecord>>(
            LeaveRecords
                .Where(item =>
                    employmentIds.Contains(item.EmploymentId)
                    && item.Status == LeaveRecordStatus.Recorded
                    && item.StartDate <= to
                    && item.EndDate >= from)
                .OrderBy(item => item.StartDate)
                .ThenBy(item => item.Id)
                .ToArray());
    }

    public void AddAttendanceCorrection(AttendanceCorrection correction) => AttendanceCorrections.Add(correction);

    public void RemoveAttendanceCorrection(AttendanceCorrection correction) =>
        AttendanceCorrections.Remove(correction);

    public void AddAttendanceCorrectionChange(AttendanceCorrectionChange change) =>
        AttendanceCorrectionChanges.Add(change);

    public Task<IWorkforceTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transactionSnapshot is not null)
        {
            throw new InvalidOperationException("Nested workforce transactions are not supported.");
        }

        _transactionSnapshot = InMemoryWorkforceSnapshot.Capture(this);
        _transactionSaveCount = 0;
        return Task.FromResult<IWorkforceTransaction>(new InMemoryWorkforceTransaction(this));
    }

    internal void CommitTransaction() => _transactionSnapshot = null;

    internal void RollbackTransaction()
    {
        if (_transactionSnapshot is not null)
        {
            _transactionSnapshot.Restore(this);
            _transactionSnapshot = null;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (_transactionSnapshot is not null)
        {
            _transactionSaveCount++;
            if (FailSaveChangesAfterCount is int limit && _transactionSaveCount >= limit)
            {
                throw new InvalidOperationException("Simulated persistence failure.");
            }
        }

        var duplicateNumber = Employees
            .GroupBy(item => (item.OrganizationId, item.PersonnelNumber))
            .Any(group => group.Count() > 1);
        if (duplicateNumber)
        {
            throw new PersonnelNumberConflictException();
        }

        var duplicateIdentity = HrProfiles
            .Where(item => item.HasNationalIdentity)
            .GroupBy(item => (
                item.OrganizationId,
                item.NationalIdentityScheme,
                item.NormalizedNationalIdentityNumber))
            .Any(group => group.Count() > 1);
        if (duplicateIdentity)
        {
            throw new NationalIdentityConflictException();
        }

        var duplicateShiftCode = ShiftDefinitions
            .GroupBy(item => (item.PropertyId, item.Code))
            .Any(group => group.Count() > 1);
        if (duplicateShiftCode)
        {
            throw new InvalidOperationException("Duplicate shift definition code for property.");
        }

        var duplicateScheduleEntry = ScheduleEntries
            .GroupBy(item => (item.EmploymentId, item.ScheduleDate))
            .Any(group => group.Count() > 1);
        if (duplicateScheduleEntry)
        {
            throw new InvalidOperationException("Schedule conflict: duplicate entry for employment and date.");
        }

        var duplicateSourceRequest = LeaveRecords
            .Where(item => item.SourceLeaveRequestId is not null)
            .GroupBy(item => item.SourceLeaveRequestId)
            .Any(group => group.Count() > 1);
        if (duplicateSourceRequest)
        {
            throw new InvalidOperationException("Leave record conflict: duplicate SourceLeaveRequestId.");
        }

        return Task.CompletedTask;
    }
}

internal sealed class InMemoryWorkforceTransaction(InMemoryWorkforceStore store) : IWorkforceTransaction
{
    private bool _completed;

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        Complete();
        store.CommitTransaction();
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken)
    {
        Complete();
        store.RollbackTransaction();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (!_completed)
        {
            store.RollbackTransaction();
        }

        return ValueTask.CompletedTask;
    }

    private void Complete() => _completed = true;
}

internal sealed class InMemoryWorkforceSnapshot
{
    private readonly List<Organization> _organizations;
    private readonly List<Property> _properties;
    private readonly List<Department> _departments;
    private readonly List<Position> _positions;
    private readonly List<DepartmentPositionApplicability> _applicabilities;
    private readonly List<Employee> _employees;
    private readonly List<Employment> _employments;
    private readonly List<Assignment> _assignments;
    private readonly List<EmployeeHrProfile> _hrProfiles;
    private readonly List<EmergencyContact> _emergencyContacts;
    private readonly List<EmployeePhoto> _photos;
    private readonly List<SgkWorkplaceRegistration> _sgkWorkplaceRegistrations;
    private readonly List<OfficialEmploymentProfile> _officialEmploymentProfiles;
    private readonly List<SgkDocumentType> _sgkDocumentTypes;
    private readonly List<ApplicableLawCode> _applicableLawCodes;
    private readonly List<InsuranceBranch> _insuranceBranches;
    private readonly List<SgkOccupationCode> _sgkOccupationCodes;
    private readonly List<EmploymentDutyCode> _employmentDutyCodes;
    private readonly List<EmploymentBesSettings> _employmentBesSettings;
    private readonly List<EmployeePaymentProfile> _paymentProfiles;
    private readonly List<PersonnelProfileChange> _profileChanges;
    private readonly List<PersonnelImportRun> _importRuns;
    private readonly List<LeaveType> _leaveTypes;
    private readonly List<LeaveEntitlement> _leaveEntitlements;
    private readonly List<LeaveRecord> _leaveRecords;
    private readonly List<(Guid Id, LeaveRecordStatus Status, DateTimeOffset? CancelledAtUtc, string? CancelledByUserId, string? CancellationReason)> _leaveRecordStates;
    private readonly List<LeaveRequest> _leaveRequests;
    private readonly List<(Guid Id, LeaveRequestStatus Status, LeaveRequestApprovalStage ApprovalStage, DateTimeOffset UpdatedAtUtc)> _leaveRequestStates;
    private readonly List<LeaveRequestDecision> _leaveRequestDecisions;
    private readonly List<ShiftDefinition> _shiftDefinitions;
    private readonly List<ScheduleEntry> _scheduleEntries;
    private readonly List<ScheduleEntryChange> _scheduleEntryChanges;
    private readonly List<AttendanceCorrection> _attendanceCorrections;
    private readonly List<AttendanceCorrectionChange> _attendanceCorrectionChanges;
    private readonly Dictionary<Guid, PersonnelNumberSequence> _sequences;
    private readonly List<PersonnelMovement> _personnelMovements = [];
    private readonly List<WorkforceReportingLine> _reportingLines = [];
    private readonly List<(Guid Id, DateOnly? EndDate)> _assignmentEndDates = [];
    private readonly List<(Guid Id, DateOnly? EffectiveTo)> _reportingLineEnds = [];
    private readonly List<(Guid Id, string? CancelledByUserId, DateTimeOffset? CancelledAtUtc, string? CancellationReason, Guid? NewAssignmentId, Guid? NewReportingLineId)> _movementCancelStates = [];

    private InMemoryWorkforceSnapshot(
        List<Organization> organizations,
        List<Property> properties,
        List<Department> departments,
        List<Position> positions,
        List<DepartmentPositionApplicability> applicabilities,
        List<Employee> employees,
        List<Employment> employments,
        List<Assignment> assignments,
        List<EmployeeHrProfile> hrProfiles,
        List<EmergencyContact> emergencyContacts,
        List<EmployeePhoto> photos,
        List<SgkWorkplaceRegistration> sgkWorkplaceRegistrations,
        List<OfficialEmploymentProfile> officialEmploymentProfiles,
        List<SgkDocumentType> sgkDocumentTypes,
        List<ApplicableLawCode> applicableLawCodes,
        List<InsuranceBranch> insuranceBranches,
        List<SgkOccupationCode> sgkOccupationCodes,
        List<EmploymentDutyCode> employmentDutyCodes,
        List<EmploymentBesSettings> employmentBesSettings,
        List<EmployeePaymentProfile> paymentProfiles,
        List<PersonnelProfileChange> profileChanges,
        List<PersonnelImportRun> importRuns,
        List<LeaveType> leaveTypes,
        List<LeaveEntitlement> leaveEntitlements,
        List<LeaveRecord> leaveRecords,
        List<(Guid Id, LeaveRecordStatus Status, DateTimeOffset? CancelledAtUtc, string? CancelledByUserId, string? CancellationReason)> leaveRecordStates,
        List<LeaveRequest> leaveRequests,
        List<(Guid Id, LeaveRequestStatus Status, LeaveRequestApprovalStage ApprovalStage, DateTimeOffset UpdatedAtUtc)> leaveRequestStates,
        List<LeaveRequestDecision> leaveRequestDecisions,
        List<ShiftDefinition> shiftDefinitions,
        List<ScheduleEntry> scheduleEntries,
        List<ScheduleEntryChange> scheduleEntryChanges,
        List<AttendanceCorrection> attendanceCorrections,
        List<AttendanceCorrectionChange> attendanceCorrectionChanges,
        Dictionary<Guid, PersonnelNumberSequence> sequences)
    {
        _organizations = organizations;
        _properties = properties;
        _departments = departments;
        _positions = positions;
        _applicabilities = applicabilities;
        _employees = employees;
        _employments = employments;
        _assignments = assignments;
        _hrProfiles = hrProfiles;
        _emergencyContacts = emergencyContacts;
        _photos = photos;
        _sgkWorkplaceRegistrations = sgkWorkplaceRegistrations;
        _officialEmploymentProfiles = officialEmploymentProfiles;
        _sgkDocumentTypes = sgkDocumentTypes;
        _applicableLawCodes = applicableLawCodes;
        _insuranceBranches = insuranceBranches;
        _sgkOccupationCodes = sgkOccupationCodes;
        _employmentDutyCodes = employmentDutyCodes;
        _employmentBesSettings = employmentBesSettings;
        _paymentProfiles = paymentProfiles;
        _profileChanges = profileChanges;
        _importRuns = importRuns;
        _leaveTypes = leaveTypes;
        _leaveEntitlements = leaveEntitlements;
        _leaveRecords = leaveRecords;
        _leaveRecordStates = leaveRecordStates;
        _leaveRequests = leaveRequests;
        _leaveRequestStates = leaveRequestStates;
        _leaveRequestDecisions = leaveRequestDecisions;
        _shiftDefinitions = shiftDefinitions;
        _scheduleEntries = scheduleEntries;
        _scheduleEntryChanges = scheduleEntryChanges;
        _attendanceCorrections = attendanceCorrections;
        _attendanceCorrectionChanges = attendanceCorrectionChanges;
        _sequences = sequences;
    }

    public static InMemoryWorkforceSnapshot Capture(InMemoryWorkforceStore store)
    {
        var snapshot = new InMemoryWorkforceSnapshot(
            [.. store.Organizations],
            [.. store.Properties],
            [.. store.Departments],
            [.. store.Positions],
            [.. store.Applicabilities],
            [.. store.Employees],
            [.. store.Employments],
            [.. store.Assignments],
            [.. store.HrProfiles],
            [.. store.EmergencyContacts],
            [.. store.Photos],
            [.. store.SgkWorkplaceRegistrations],
            [.. store.OfficialEmploymentProfiles],
            [.. store.SgkDocumentTypes],
            [.. store.ApplicableLawCodes],
            [.. store.InsuranceBranches],
            [.. store.SgkOccupationCodes],
            [.. store.EmploymentDutyCodes],
            [.. store.EmploymentBesSettings],
            [.. store.PaymentProfiles],
            [.. store.ProfileChanges],
            [.. store.ImportRuns],
            [.. store.LeaveTypes],
            [.. store.LeaveEntitlements],
            [.. store.LeaveRecords],
            store.LeaveRecords
                .Select(item => (item.Id, item.Status, item.CancelledAtUtc, item.CancelledByUserId, item.CancellationReason))
                .ToList(),
            [.. store.LeaveRequests],
            store.LeaveRequests
                .Select(item => (item.Id, item.Status, item.ApprovalStage, item.UpdatedAtUtc))
                .ToList(),
            [.. store.LeaveRequestDecisions],
            [.. store.ShiftDefinitions],
            [.. store.ScheduleEntries],
            [.. store.ScheduleEntryChanges],
            [.. store.AttendanceCorrections],
            [.. store.AttendanceCorrectionChanges],
            store.Sequences.ToDictionary(item => item.Key, item => item.Value));
        snapshot.CaptureTemporal(store);
        return snapshot;
    }

    private void CaptureTemporal(InMemoryWorkforceStore store)
    {
        _personnelMovements.AddRange(store.PersonnelMovements);
        _reportingLines.AddRange(store.ReportingLines);
        _assignmentEndDates.AddRange(store.Assignments.Select(item => (item.Id, item.EndDate)));
        _reportingLineEnds.AddRange(store.ReportingLines.Select(item => (item.Id, item.EffectiveTo)));
        _movementCancelStates.AddRange(store.PersonnelMovements.Select(item => (
            item.Id,
            item.CancelledByUserId,
            item.CancelledAtUtc,
            item.CancellationReason,
            item.NewAssignmentId,
            item.NewReportingLineId)));
    }

    public void Restore(InMemoryWorkforceStore store)
    {
        Replace(store.Organizations, _organizations);
        Replace(store.Properties, _properties);
        Replace(store.Departments, _departments);
        Replace(store.Positions, _positions);
        Replace(store.Applicabilities, _applicabilities);
        Replace(store.Employees, _employees);
        Replace(store.Employments, _employments);
        Replace(store.Assignments, _assignments);
        foreach (var state in _assignmentEndDates)
        {
            store.Assignments.FirstOrDefault(item => item.Id == state.Id)?.RestoreEndDate(state.EndDate);
        }

        Replace(store.PersonnelMovements, _personnelMovements);
        foreach (var state in _movementCancelStates)
        {
            var movement = store.PersonnelMovements.FirstOrDefault(item => item.Id == state.Id);
            movement?.RestoreCancellationState(state.CancelledByUserId, state.CancelledAtUtc, state.CancellationReason);
            movement?.RestoreSuccessorIds(state.NewAssignmentId, state.NewReportingLineId);
        }

        Replace(store.ReportingLines, _reportingLines);
        foreach (var state in _reportingLineEnds)
        {
            store.ReportingLines.FirstOrDefault(item => item.Id == state.Id)?.RestoreEffectiveTo(state.EffectiveTo);
        }

        Replace(store.HrProfiles, _hrProfiles);
        Replace(store.EmergencyContacts, _emergencyContacts);
        Replace(store.Photos, _photos);
        Replace(store.SgkWorkplaceRegistrations, _sgkWorkplaceRegistrations);
        Replace(store.OfficialEmploymentProfiles, _officialEmploymentProfiles);
        Replace(store.SgkDocumentTypes, _sgkDocumentTypes);
        Replace(store.ApplicableLawCodes, _applicableLawCodes);
        Replace(store.InsuranceBranches, _insuranceBranches);
        Replace(store.SgkOccupationCodes, _sgkOccupationCodes);
        Replace(store.EmploymentDutyCodes, _employmentDutyCodes);
        Replace(store.EmploymentBesSettings, _employmentBesSettings);
        Replace(store.PaymentProfiles, _paymentProfiles);
        Replace(store.ProfileChanges, _profileChanges);
        Replace(store.ImportRuns, _importRuns);
        Replace(store.LeaveTypes, _leaveTypes);
        Replace(store.LeaveEntitlements, _leaveEntitlements);
        Replace(store.LeaveRecords, _leaveRecords);
        foreach (var state in _leaveRecordStates)
        {
            var record = store.LeaveRecords.FirstOrDefault(item => item.Id == state.Id);
            record?.RestoreCancellationState(
                state.Status,
                state.CancelledAtUtc,
                state.CancelledByUserId,
                state.CancellationReason);
        }

        Replace(store.LeaveRequests, _leaveRequests);
        foreach (var state in _leaveRequestStates)
        {
            var request = store.LeaveRequests.FirstOrDefault(item => item.Id == state.Id);
            request?.RestoreWorkflowState(state.Status, state.ApprovalStage, state.UpdatedAtUtc);
        }

        Replace(store.LeaveRequestDecisions, _leaveRequestDecisions);
        Replace(store.ShiftDefinitions, _shiftDefinitions);
        Replace(store.ScheduleEntries, _scheduleEntries);
        Replace(store.ScheduleEntryChanges, _scheduleEntryChanges);
        Replace(store.AttendanceCorrections, _attendanceCorrections);
        Replace(store.AttendanceCorrectionChanges, _attendanceCorrectionChanges);
        store.Sequences.Clear();
        foreach (var (key, value) in _sequences)
        {
            store.Sequences[key] = value;
        }
    }

    private static void Replace<T>(List<T> target, List<T> source)
    {
        target.Clear();
        target.AddRange(source);
    }
}

internal sealed class InMemoryEmployeePhotoStorage : IEmployeePhotoStorage
{
    public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);

    public Task SaveAsync(string storageKey, byte[] content, CancellationToken cancellationToken)
    {
        Files[storageKey] = content;
        return Task.CompletedTask;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        if (!Files.TryGetValue(storageKey, out var bytes))
        {
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(new MemoryStream(bytes, writable: false));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        Files.Remove(storageKey);
        return Task.CompletedTask;
    }
}

internal sealed class WorkforceHarness
{
    public Guid OrganizationId { get; } = Guid.CreateVersion7();
    public Guid PropertyId { get; } = Guid.CreateVersion7();
    public Guid DepartmentId { get; } = Guid.CreateVersion7();
    public Guid PositionId { get; } = Guid.CreateVersion7();
    public Guid InactiveDepartmentId { get; } = Guid.CreateVersion7();
    public Guid InactivePositionId { get; } = Guid.CreateVersion7();
    public Guid OtherDepartmentId { get; } = Guid.CreateVersion7();
    public Guid OtherPositionId { get; } = Guid.CreateVersion7();

    public FakeClock Clock { get; } = new();
    public InMemoryWorkforceStore Store { get; } = new();
    public FixedWorkplace Workplace { get; }

    public HireEmployeeUseCase Hire { get; }
    public HireEmployeeWithProfileUseCase HireWithProfile { get; }
    public UpdateEmployeeHrProfileUseCase UpdateProfile { get; }
    public TransferEmployeeUseCase Transfer { get; }
    public CreateWorkforceMovementUseCase CreateMovement { get; }
    public CancelWorkforceMovementUseCase CancelMovement { get; }
    public ListPersonnelMovementsQuery ListMovements { get; }
    public GetPersonnelMovementQuery GetMovement { get; }
    public EndEmploymentUseCase EndEmployment { get; }
    public ActiveWorkforceQuery ActiveWorkforce { get; }
    public EmployeeHistoryQuery History { get; }
    public HrEmployeeDirectoryQuery HrDirectory { get; }
    public HrEmployeeCardQuery HrCard { get; }
    public EmployeePhotoUseCases Photos { get; }
    public InMemoryEmployeePhotoStorage PhotoStorage { get; } = new();
    public MaintainSgkWorkplaceRegistrationsUseCase SgkWorkplaces { get; }
    public MaintainDepartmentsUseCase Departments { get; }
    public MaintainPositionsUseCase Positions { get; }
    public SaveOfficialEmploymentProfileUseCase SaveOfficial { get; }
    public OfficialLookupsQuery OfficialLookups { get; }
    public EnsureDefaultLeaveTypesUseCase EnsureDefaultLeaveTypes { get; }
    public EnsurePersonnelEnrichmentDefaultsUseCase EnsurePersonnelEnrichmentDefaults { get; }
    public LeaveTypeAdminUseCase LeaveTypeAdmin { get; }
    public OnboardingCatalogQuery OnboardingCatalog { get; }
    public OnboardingChecklistQuery OnboardingChecklist { get; }
    public SyncOnboardingChecklistUseCase SyncOnboardingChecklist { get; }
    public ListOnboardingDocumentRequirementsQuery ListOnboardingRequirements { get; }
    public SetOnboardingChecklistItemUseCase SetOnboardingChecklistItem { get; }
    public CompleteEmploymentOnboardingUseCase CompleteEmploymentOnboarding { get; }
    public RenderHrDocumentDocxUseCase RenderHrDocumentDocx { get; }
    public HrDocumentTemplateQuery HrDocumentTemplates { get; }
    public PreviewHrDocumentTemplateUseCase PreviewHrDocumentTemplate { get; }
    public PreviewHrDocumentTemplateDraftUseCase PreviewHrDocumentTemplateDraft { get; }
    public RenderHrDocumentDraftDocxUseCase RenderHrDocumentDraftDocx { get; }
    public ListRecruitmentSourcesQuery ListRecruitmentSources { get; }
    public EmployeeLeaveQuery LeaveQuery { get; }
    public RecordLeaveEntitlementUseCase RecordLeaveEntitlement { get; }
    public RecordLeaveUseCase RecordLeave { get; }
    public CancelLeaveRecordUseCase CancelLeaveRecord { get; }
    public CreateLeaveRequestUseCase CreateLeaveRequest { get; }
    public ApproveLeaveRequestDepartmentUseCase ApproveLeaveRequestDepartment { get; }
    public ApproveLeaveRequestHrUseCase ApproveLeaveRequestHr { get; }
    public RejectLeaveRequestUseCase RejectLeaveRequest { get; }
    public WithdrawLeaveRequestUseCase WithdrawLeaveRequest { get; }
    public CancelApprovedLeaveRequestUseCase CancelApprovedLeaveRequest { get; }
    public LeaveRequestComposer LeaveRequestComposer { get; }
    public LeaveRequestQuery LeaveRequestQuery { get; }
    public CreateMyLeaveRequestUseCase CreateMyLeaveRequest { get; }
    public PreviewLeaveRequestUseCase PreviewLeaveRequest { get; }
    public LeaveRequestActionUseCase LeaveRequestActions { get; }
    public ShiftDefinitionAdminUseCase ShiftDefinitionAdmin { get; }
    public UpsertScheduleEntryUseCase UpsertSchedule { get; }
    public ClearScheduleEntryUseCase ClearSchedule { get; }
    public GetScheduleStateQuery GetScheduleState { get; }
    public GetScheduleRangeQuery GetScheduleRange { get; }
    public GetScheduleWeekQuery GetScheduleWeek { get; }
    public BulkScheduleUseCase BulkSchedule { get; }
    public CopyScheduleWeekUseCase CopyScheduleWeek { get; }
    public GetAttendanceMonthQuery GetAttendanceMonth { get; }
    public SetAttendanceCorrectionUseCase SetAttendanceCorrection { get; }
    public ClearAttendanceCorrectionUseCase ClearAttendanceCorrection { get; }
    public GetAttendanceCorrectionHistoryQuery GetAttendanceHistory { get; }
    public Guid OtherPropertyId { get; } = Guid.CreateVersion7();
    public Guid OtherPropertyDepartmentId { get; } = Guid.CreateVersion7();
    public Guid OtherPropertyPositionId { get; } = Guid.CreateVersion7();

    public WorkforceHarness(bool withoutPropertyContext = false)
    {
        Workplace = withoutPropertyContext
            ? new FixedWorkplace(OrganizationId, Guid.Empty)
            : new FixedWorkplace(OrganizationId, PropertyId);
        Store.Organizations.Add(new Organization(OrganizationId, "Test Organization"));
        Store.Properties.Add(new Property(PropertyId, OrganizationId, "Test Property", "UTC"));

        AddDepartment(DepartmentId, "Kat Hizmetleri", active: true);
        AddDepartment(InactiveDepartmentId, "Kapalı Departman", active: false);
        AddDepartment(OtherDepartmentId, "Ön Büro", active: true);
        AddPosition(PositionId, "Kat Görevlisi", active: true);
        AddPosition(InactivePositionId, "Kapalı Pozisyon", active: false);
        AddPosition(OtherPositionId, "Resepsiyon Görevlisi", active: true);
        AddApplicability(DepartmentId, PositionId);
        AddApplicability(OtherDepartmentId, PositionId);
        AddApplicability(OtherDepartmentId, OtherPositionId);

        foreach (var (code, description) in OfficialLookupCatalog.DocumentTypes)
        {
            Store.SgkDocumentTypes.Add(new SgkDocumentType(code, description));
        }

        foreach (var (code, description) in OfficialLookupCatalog.ApplicableLaws)
        {
            Store.ApplicableLawCodes.Add(new ApplicableLawCode(code, description));
        }

        foreach (var (code, description) in OfficialLookupCatalog.InsuranceBranches)
        {
            Store.InsuranceBranches.Add(new InsuranceBranch(code, description));
        }

        foreach (var (code, description) in OfficialLookupCatalog.DutyCodes)
        {
            Store.EmploymentDutyCodes.Add(new EmploymentDutyCode(code, description));
        }

        foreach (var (code, description) in TestOccupationSeed)
        {
            Store.SgkOccupationCodes.Add(
                new SgkOccupationCode(
                    code,
                    description,
                    isActive: true,
                    OfficialLookupCatalog.OccupationCatalogueSource,
                    OfficialLookupCatalog.OccupationCatalogueVersion));
        }

        Store.Properties.Add(new Property(OtherPropertyId, OrganizationId, "Other Property", "UTC"));
        Assert.True(Department.TryCreate(
            OtherPropertyDepartmentId,
            OtherPropertyId,
            "Other Property Dept",
            code: null,
            out var otherDept,
            out _));
        Store.Departments.Add(otherDept!);
        Assert.True(Position.TryCreate(
            OtherPropertyPositionId,
            OtherPropertyId,
            "Other Property Position",
            code: null,
            out var otherPos,
            out _));
        Store.Positions.Add(otherPos!);
        AddApplicability(OtherPropertyDepartmentId, OtherPropertyPositionId);

        Hire = new HireEmployeeUseCase(Store, Clock, Workplace);
        HireWithProfile = new HireEmployeeWithProfileUseCase(Store, Clock, Workplace);
        UpdateProfile = new UpdateEmployeeHrProfileUseCase(Store, Clock, Workplace);
        Transfer = new TransferEmployeeUseCase(Store, Clock, Workplace);
        CreateMovement = new CreateWorkforceMovementUseCase(Store, Clock, Workplace);
        CancelMovement = new CancelWorkforceMovementUseCase(Store, Clock, Workplace);
        ListMovements = new ListPersonnelMovementsQuery(Store, Clock, Workplace);
        GetMovement = new GetPersonnelMovementQuery(Store, Clock, Workplace);
        EndEmployment = new EndEmploymentUseCase(Store, Workplace);
        ActiveWorkforce = new ActiveWorkforceQuery(Store, Clock, Workplace);
        History = new EmployeeHistoryQuery(Store, Clock, Workplace);
        HrDirectory = new HrEmployeeDirectoryQuery(Store, Clock, Workplace);
        HrCard = new HrEmployeeCardQuery(Store, Clock, Workplace);
        Photos = new EmployeePhotoUseCases(Store, Workplace, PhotoStorage, Clock);
        SgkWorkplaces = new MaintainSgkWorkplaceRegistrationsUseCase(Store, Workplace, Clock);
        Departments = new MaintainDepartmentsUseCase(Store, Workplace);
        Positions = new MaintainPositionsUseCase(Store, Workplace);
        SaveOfficial = new SaveOfficialEmploymentProfileUseCase(Store, Clock, Workplace);
        OfficialLookups = new OfficialLookupsQuery(Store);
        EnsureDefaultLeaveTypes = new EnsureDefaultLeaveTypesUseCase(Store, Clock);
        EnsurePersonnelEnrichmentDefaults = new EnsurePersonnelEnrichmentDefaultsUseCase(Store, Clock);
        LeaveTypeAdmin = new LeaveTypeAdminUseCase(Store, Clock, Workplace);
        OnboardingCatalog = new OnboardingCatalogQuery(Store, Workplace, EnsurePersonnelEnrichmentDefaults);
        OnboardingChecklist = new OnboardingChecklistQuery(Store, Workplace, EnsurePersonnelEnrichmentDefaults);
        SyncOnboardingChecklist = new SyncOnboardingChecklistUseCase(Store, Clock, Workplace, OnboardingChecklist);
        ListOnboardingRequirements = new ListOnboardingDocumentRequirementsQuery(Store, Workplace, EnsurePersonnelEnrichmentDefaults);
        SetOnboardingChecklistItem = new SetOnboardingChecklistItemUseCase(Store, Clock, Workplace);
        CompleteEmploymentOnboarding = new CompleteEmploymentOnboardingUseCase(Store, Workplace, OnboardingChecklist);
        RenderHrDocumentDocx = new RenderHrDocumentDocxUseCase(Store, Workplace);
        HrDocumentTemplates = new HrDocumentTemplateQuery(Store, Workplace);
        PreviewHrDocumentTemplate = new PreviewHrDocumentTemplateUseCase(Store, Clock, Workplace);
        PreviewHrDocumentTemplateDraft = new PreviewHrDocumentTemplateDraftUseCase(Store, Clock, Workplace);
        RenderHrDocumentDraftDocx = new RenderHrDocumentDraftDocxUseCase(Store, Clock, Workplace);
        ListRecruitmentSources = new ListRecruitmentSourcesQuery(Store, Workplace);
        LeaveQuery = new EmployeeLeaveQuery(Store, Clock, Workplace);
        RecordLeaveEntitlement = new RecordLeaveEntitlementUseCase(Store, Clock, Workplace, LeaveQuery);
        RecordLeave = new RecordLeaveUseCase(Store, Clock, Workplace, LeaveQuery);
        CancelLeaveRecord = new CancelLeaveRecordUseCase(Store, Clock, Workplace, LeaveQuery);
        CreateLeaveRequest = new CreateLeaveRequestUseCase(Store, Clock, Workplace);
        ApproveLeaveRequestDepartment = new ApproveLeaveRequestDepartmentUseCase(Store, Clock);
        ApproveLeaveRequestHr = new ApproveLeaveRequestHrUseCase(Store, Clock);
        RejectLeaveRequest = new RejectLeaveRequestUseCase(Store, Clock);
        WithdrawLeaveRequest = new WithdrawLeaveRequestUseCase(Store, Clock);
        CancelApprovedLeaveRequest = new CancelApprovedLeaveRequestUseCase(Store, Clock);
        LeaveRequestComposer = new LeaveRequestComposer(Store);
        LeaveRequestQuery = new LeaveRequestQuery(Store, Workplace, LeaveRequestComposer);
        CreateMyLeaveRequest = new CreateMyLeaveRequestUseCase(Store, CreateLeaveRequest, LeaveRequestComposer);
        PreviewLeaveRequest = new PreviewLeaveRequestUseCase(Store, Workplace, LeaveRequestComposer);
        LeaveRequestActions = new LeaveRequestActionUseCase(
            LeaveRequestQuery,
            LeaveRequestComposer,
            ApproveLeaveRequestDepartment,
            ApproveLeaveRequestHr,
            RejectLeaveRequest,
            WithdrawLeaveRequest,
            CancelApprovedLeaveRequest);
        ShiftDefinitionAdmin = new ShiftDefinitionAdminUseCase(Store, Clock, Workplace);
        UpsertSchedule = new UpsertScheduleEntryUseCase(Store, Clock, Workplace);
        ClearSchedule = new ClearScheduleEntryUseCase(Store, Clock, Workplace);
        GetScheduleState = new GetScheduleStateQuery(Store, Workplace);
        GetScheduleRange = new GetScheduleRangeQuery(Store, Workplace);
        GetScheduleWeek = new GetScheduleWeekQuery(Store, Workplace);
        BulkSchedule = new BulkScheduleUseCase(Store, UpsertSchedule, ClearSchedule);
        CopyScheduleWeek = new CopyScheduleWeekUseCase(Store, Workplace, GetScheduleWeek, BulkSchedule);
        GetAttendanceMonth = new GetAttendanceMonthQuery(Store, Workplace);
        SetAttendanceCorrection = new SetAttendanceCorrectionUseCase(Store, Clock, Workplace);
        ClearAttendanceCorrection = new ClearAttendanceCorrectionUseCase(Store, Clock, Workplace);
        GetAttendanceHistory = new GetAttendanceCorrectionHistoryQuery(Store, Workplace);
    }

    public async Task<Guid> SeedDefaultLeaveTypesAsync()
    {
        await EnsureDefaultLeaveTypes.ExecuteAsync(OrganizationId, CancellationToken.None);
        return OrganizationId;
    }

    public LeaveType SeedLeaveType(
        string code,
        string name,
        bool tracksBalance,
        LeaveTypeSystemKind? systemKind = null,
        bool active = true)
    {
        LeaveType leaveType;
        if (systemKind is { } kind)
        {
            leaveType = LeaveType.CreateSystemDefault(
                Guid.CreateVersion7(),
                OrganizationId,
                code,
                name,
                kind,
                tracksBalance,
                "seed-actor",
                Clock.UtcNow);
        }
        else
        {
            Assert.True(LeaveType.TryCreateCustom(
                Guid.CreateVersion7(),
                OrganizationId,
                code,
                name,
                tracksBalance,
                "seed-actor",
                Clock.UtcNow,
                out var created,
                out _,
                out _));
            leaveType = created!;
        }

        if (!active)
        {
            leaveType.Deactivate("seed-actor", Clock.UtcNow);
        }

        Store.LeaveTypes.Add(leaveType);
        return leaveType;
    }

    public async Task<(Guid EmployeeId, Guid EmploymentId)> SeedEmploymentAsync(
        DateOnly? startDate = null)
    {
        var hired = await Hire.ExecuteAsync(
            HireCommand(startDate: startDate ?? Clock.Today.AddDays(-30)),
            CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        return (hired.Value!.EmployeeId, hired.Value.EmploymentId);
    }

    public HireEmployeeCommand HireCommand(
        DateOnly? startDate = null,
        Guid? departmentId = null,
        Guid? positionId = null) =>
        new("Ayşe", "Yılmaz", startDate ?? Clock.Today, departmentId ?? DepartmentId, positionId ?? PositionId);

    public HireEmployeeWithProfileCommand HireWithProfileCommand(
        DateOnly? startDate = null,
        Guid? departmentId = null,
        Guid? positionId = null,
        HrProfileWriteModel? profile = null,
        bool canWriteSensitive = true,
        OfficialEmploymentWriteModel? officialProfile = null,
        EmploymentWorkforceWriteModel? workforceTerms = null,
        EmploymentBesWriteModel? besSettings = null,
        DateOnly? seniorityStartDate = null,
        IReadOnlyList<EmployeeCertificateDraft>? certificates = null) =>
        new(
            "Ayşe",
            "Yılmaz",
            startDate ?? Clock.Today,
            departmentId ?? DepartmentId,
            positionId ?? PositionId,
            profile ?? EmptyProfile(),
            canWriteSensitive,
            officialProfile,
            workforceTerms,
            besSettings,
            seniorityStartDate,
            certificates);

    public OfficialEmploymentWriteModel OfficialWrite(
        Guid? workplaceId = null,
        string? documentType = null,
        string? law = null,
        string? insurance = null,
        string? occupation = null,
        string? dutyCode = null) =>
        new(workplaceId, documentType, law, insurance, occupation, dutyCode);

    public SgkWorkplaceRegistration SeedWorkplace(
        Guid? propertyId = null,
        string registrationNumber = "123456789012345678901",
        string? displayName = "Otel",
        bool active = true)
    {
        Assert.True(SgkWorkplaceRegistration.TryCreate(
            Guid.CreateVersion7(),
            propertyId ?? PropertyId,
            registrationNumber,
            displayName,
            Clock.UtcNow,
            out var registration,
            out _,
            out _));
        if (!active)
        {
            registration!.Deactivate();
        }

        Store.SgkWorkplaceRegistrations.Add(registration!);
        return registration!;
    }

    public Employee SeedEmployee(string personnelNumber, string givenName = "Seed", string familyName = "Person")
    {
        Assert.True(Employee.TryCreate(
            Guid.CreateVersion7(),
            OrganizationId,
            givenName,
            familyName,
            personnelNumber,
            out var employee,
            out _));
        Store.Employees.Add(employee!);
        return employee!;
    }

    public void AddApplicability(Guid departmentId, Guid positionId) =>
        Store.Applicabilities.Add(new DepartmentPositionApplicability(departmentId, positionId));

    public static HrProfileWriteModel EmptyProfile() =>
        new(
            null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, []);

    private static readonly (string Code, string Description)[] TestOccupationSeed =
    [
        ("0110.00", "Subaylar"),
        ("1120.10", "Genel Müdür-Eğlence, Lokanta, Otel"),
        ("1411.02", "Ön Büro Müdürü-Otel"),
        ("1411.08", "Otel Müdürü"),
        ("3434.01", "Aşçıbaşı"),
        ("4224.03", "Ön Büro Görevlisi (Otel Resepsiyoncusu)"),
        ("5120.10", "Aşçı")
    ];

    private void AddDepartment(Guid id, string name, bool active)
    {
        Assert.True(Department.TryCreate(id, PropertyId, name, null, out var department, out _));
        if (!active)
        {
            department!.Deactivate();
        }

        Store.Departments.Add(department!);
    }

    private void AddPosition(Guid id, string name, bool active)
    {
        Assert.True(Position.TryCreate(id, PropertyId, name, null, out var position, out _));
        if (!active)
        {
            position!.Deactivate();
        }

        Store.Positions.Add(position!);
    }
}
