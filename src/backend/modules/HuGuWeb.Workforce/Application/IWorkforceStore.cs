using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public interface IWorkplaceContext
{
    Guid OrganizationId { get; }
    Guid PropertyId { get; }
    bool HasOrganization { get; }
    bool HasProperty { get; }
    bool IsConfigured { get; }
}

public interface IWorkforceStore
{
    Task<Organization?> GetOrganizationAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ListOrganizationIdsAsync(CancellationToken cancellationToken);
    Task<Property?> GetPropertyAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Property>> ListPropertiesAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<Department?> GetDepartmentAsync(Guid id, CancellationToken cancellationToken);
    Task<Position?> GetPositionAsync(Guid id, CancellationToken cancellationToken);
    Task<Employee?> GetEmployeeAsync(Guid id, CancellationToken cancellationToken);
    Task<Employee?> FindEmployeeByPersonnelNumberAsync(
        Guid organizationId,
        string personnelNumber,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Department>> ListDepartmentsAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Department>> ListDepartmentsForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Position>> ListPositionsAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Position>> ListPositionsForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<DepartmentPositionApplicability>> ListApplicabilitiesForPositionsAsync(
        IReadOnlyCollection<Guid> positionIds,
        CancellationToken cancellationToken);
    Task<bool> IsPositionApplicableToDepartmentAsync(
        Guid departmentId,
        Guid positionId,
        CancellationToken cancellationToken);
    Task<string> AllocatePersonnelNumberAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Employee>> ListEmployeesAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Employment>> ListEmploymentsAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Employment>> ListEmploymentsForEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Assignment>> ListAssignmentsAsync(Guid employmentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Assignment>> ListAssignmentsForEmploymentsAsync(
        IReadOnlyCollection<Guid> employmentIds,
        CancellationToken cancellationToken);

    void AddDepartment(Department department);
    void AddPosition(Position position);
    void AddApplicability(DepartmentPositionApplicability applicability);
    void RemoveApplicability(DepartmentPositionApplicability applicability);
    void AddEmployee(Employee employee);
    void AddEmployment(Employment employment);
    void AddAssignment(Assignment assignment);

    Task<EmployeeHrProfile?> GetHrProfileAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeeHrProfile>> ListHrProfilesForEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken);
    Task<EmployeeHrProfile?> FindHrProfileByNationalIdentityAsync(
        Guid organizationId,
        NationalIdentityScheme scheme,
        string normalizedNationalIdentityNumber,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<EmergencyContact>> ListEmergencyContactsAsync(
        Guid employeeId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<EmergencyContact>> ListEmergencyContactsForEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken);
    Task<EmployeePhoto?> GetEmployeePhotoAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeePhoto>> ListEmployeePhotosForEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken);

    void AddHrProfile(EmployeeHrProfile profile);
    void AddEmergencyContact(EmergencyContact contact);
    void RemoveEmergencyContact(EmergencyContact contact);
    void AddEmployeePhoto(EmployeePhoto photo);
    void RemoveEmployeePhoto(EmployeePhoto photo);

    Task<SgkWorkplaceRegistration?> GetSgkWorkplaceRegistrationAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<SgkWorkplaceRegistration>> ListSgkWorkplaceRegistrationsAsync(
        Guid propertyId,
        CancellationToken cancellationToken);
    void AddSgkWorkplaceRegistration(SgkWorkplaceRegistration registration);

    Task<OfficialEmploymentProfile?> GetOfficialEmploymentProfileAsync(
        Guid employmentId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<OfficialEmploymentProfile>> ListOfficialEmploymentProfilesForEmploymentsAsync(
        IReadOnlyCollection<Guid> employmentIds,
        CancellationToken cancellationToken);
    void AddOfficialEmploymentProfile(OfficialEmploymentProfile profile);

    Task<SgkDocumentType?> GetSgkDocumentTypeAsync(string code, CancellationToken cancellationToken);
    Task<IReadOnlyList<SgkDocumentType>> ListSgkDocumentTypesAsync(CancellationToken cancellationToken);
    Task<ApplicableLawCode?> GetApplicableLawCodeAsync(string code, CancellationToken cancellationToken);
    Task<IReadOnlyList<ApplicableLawCode>> ListApplicableLawCodesAsync(CancellationToken cancellationToken);
    Task<InsuranceBranch?> GetInsuranceBranchAsync(string code, CancellationToken cancellationToken);
    Task<IReadOnlyList<InsuranceBranch>> ListInsuranceBranchesAsync(CancellationToken cancellationToken);
    Task<SgkOccupationCode?> GetSgkOccupationCodeAsync(string code, CancellationToken cancellationToken);
    Task<IReadOnlyList<SgkOccupationCode>> SearchSgkOccupationCodesAsync(
        string? query,
        int take,
        CancellationToken cancellationToken);
    Task<EmploymentDutyCode?> GetEmploymentDutyCodeAsync(string code, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmploymentDutyCode>> ListEmploymentDutyCodesAsync(CancellationToken cancellationToken);

    Task<EmploymentBesSettings?> GetEmploymentBesSettingsAsync(
        Guid employmentId,
        CancellationToken cancellationToken);
    void AddEmploymentBesSettings(EmploymentBesSettings settings);

    Task<EmployeePaymentProfile?> GetPaymentProfileAsync(Guid employeeId, CancellationToken cancellationToken);
    void AddPaymentProfile(EmployeePaymentProfile profile);

    Task<IReadOnlyList<PersonnelProfileChange>> ListPersonnelProfileChangesAsync(
        Guid employeeId,
        CancellationToken cancellationToken);

    void AddPersonnelProfileChange(PersonnelProfileChange change);
    void AddPersonnelImportRun(PersonnelImportRun importRun);

    Task<IReadOnlyList<LeaveType>> ListLeaveTypesAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<LeaveType?> GetLeaveTypeAsync(Guid id, CancellationToken cancellationToken);
    Task<LeaveType?> FindLeaveTypeByCodeAsync(
        Guid organizationId,
        string normalizedCode,
        CancellationToken cancellationToken);
    Task<bool> LeaveTypeHasUsageAsync(Guid leaveTypeId, CancellationToken cancellationToken);
    void AddLeaveType(LeaveType leaveType);

    Task<IReadOnlyList<LeaveEntitlement>> ListLeaveEntitlementsAsync(
        Guid employmentId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<LeaveRecord>> ListLeaveRecordsAsync(Guid employmentId, CancellationToken cancellationToken);
    Task<LeaveRecord?> GetLeaveRecordAsync(Guid id, CancellationToken cancellationToken);
    void AddLeaveEntitlement(LeaveEntitlement entitlement);
    void AddLeaveRecord(LeaveRecord record);

    Task<IWorkforceTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
