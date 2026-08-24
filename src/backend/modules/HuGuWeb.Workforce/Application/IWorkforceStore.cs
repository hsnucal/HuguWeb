using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public interface IWorkplaceContext
{
    Guid OrganizationId { get; }
    Guid PropertyId { get; }
    bool IsConfigured { get; }
}

public interface IWorkforceStore
{
    Task<Organization?> GetOrganizationAsync(Guid id, CancellationToken cancellationToken);
    Task<Property?> GetPropertyAsync(Guid id, CancellationToken cancellationToken);
    Task<Department?> GetDepartmentAsync(Guid id, CancellationToken cancellationToken);
    Task<Position?> GetPositionAsync(Guid id, CancellationToken cancellationToken);
    Task<Employee?> GetEmployeeAsync(Guid id, CancellationToken cancellationToken);
    Task<Employee?> FindEmployeeByPersonnelNumberAsync(
        Guid organizationId,
        string personnelNumber,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Department>> ListDepartmentsAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Position>> ListPositionsAsync(Guid propertyId, CancellationToken cancellationToken);
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

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
