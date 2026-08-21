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
    void AddEmployee(Employee employee);
    void AddEmployment(Employment employment);
    void AddAssignment(Assignment assignment);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
