using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

internal sealed class FakeClock : IWorkforceClock
{
    public DateOnly Today { get; set; } = new(2026, 8, 21);
}

internal sealed class FixedWorkplace(Guid organizationId, Guid propertyId) : IWorkplaceContext
{
    public Guid OrganizationId { get; } = organizationId;
    public Guid PropertyId { get; } = propertyId;
    public bool IsConfigured => true;
}

internal sealed class InMemoryWorkforceStore : IWorkforceStore
{
    public List<Organization> Organizations { get; } = [];
    public List<Property> Properties { get; } = [];
    public List<Department> Departments { get; } = [];
    public List<Position> Positions { get; } = [];
    public List<Employee> Employees { get; } = [];
    public List<Employment> Employments { get; } = [];
    public List<Assignment> Assignments { get; } = [];

    public Task<Organization?> GetOrganizationAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Organizations.FirstOrDefault(item => item.Id == id));

    public Task<Property?> GetPropertyAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Properties.FirstOrDefault(item => item.Id == id));

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

    public Task<IReadOnlyList<Position>> ListPositionsAsync(
        Guid propertyId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Position>>(
            Positions.Where(item => item.PropertyId == propertyId).ToArray());

    public Task<IReadOnlyList<Employee>> ListEmployeesAsync(Guid organizationId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Employee>>(Employees.Where(item => item.OrganizationId == organizationId).ToArray());

    public Task<IReadOnlyList<Employment>> ListEmploymentsAsync(Guid employeeId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Employment>>(Employments.Where(item => item.EmployeeId == employeeId).ToArray());

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

    public void AddDepartment(Department department) => Departments.Add(department);

    public void AddPosition(Position position) => Positions.Add(position);

    public void AddEmployee(Employee employee) => Employees.Add(employee);

    public void AddEmployment(Employment employment) => Employments.Add(employment);

    public void AddAssignment(Assignment assignment) => Assignments.Add(assignment);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        var duplicate = Employees
            .GroupBy(item => (item.OrganizationId, item.PersonnelNumber))
            .Any(group => group.Count() > 1);
        if (duplicate)
        {
            throw new PersonnelNumberConflictException();
        }

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
    public TransferEmployeeUseCase Transfer { get; }
    public EndEmploymentUseCase EndEmployment { get; }
    public ActiveWorkforceQuery ActiveWorkforce { get; }
    public EmployeeHistoryQuery History { get; }

    public WorkforceHarness()
    {
        Workplace = new FixedWorkplace(OrganizationId, PropertyId);
        Store.Organizations.Add(new Organization(OrganizationId, "Test Organization"));
        Store.Properties.Add(new Property(PropertyId, OrganizationId, "Test Property"));

        AddDepartment(DepartmentId, "Kat Hizmetleri", active: true);
        AddDepartment(InactiveDepartmentId, "Kapalı Departman", active: false);
        AddDepartment(OtherDepartmentId, "Ön Büro", active: true);
        AddPosition(PositionId, "Kat Görevlisi", active: true);
        AddPosition(InactivePositionId, "Kapalı Pozisyon", active: false);
        AddPosition(OtherPositionId, "Resepsiyon Görevlisi", active: true);

        Hire = new HireEmployeeUseCase(Store, Clock, Workplace);
        Transfer = new TransferEmployeeUseCase(Store, Clock, Workplace);
        EndEmployment = new EndEmploymentUseCase(Store, Workplace);
        ActiveWorkforce = new ActiveWorkforceQuery(Store, Clock, Workplace);
        History = new EmployeeHistoryQuery(Store, Clock, Workplace);
    }

    public HireEmployeeCommand HireCommand(
        string personnelNumber = "P-1001",
        DateOnly? startDate = null,
        Guid? departmentId = null,
        Guid? positionId = null) =>
        new("Ayşe", "Yılmaz", personnelNumber, startDate ?? Clock.Today, departmentId ?? DepartmentId, positionId ?? PositionId);

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
