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
    public List<DepartmentPositionApplicability> Applicabilities { get; } = [];
    public Dictionary<Guid, PersonnelNumberSequence> Sequences { get; } = [];
    public List<Employee> Employees { get; } = [];
    public List<Employment> Employments { get; } = [];
    public List<Assignment> Assignments { get; } = [];
    public List<EmployeeHrProfile> HrProfiles { get; } = [];
    public List<EmergencyContact> EmergencyContacts { get; } = [];
    public List<EmployeePhoto> Photos { get; } = [];

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

    public void AddApplicability(DepartmentPositionApplicability applicability) =>
        Applicabilities.Add(applicability);

    public void RemoveApplicability(DepartmentPositionApplicability applicability) =>
        Applicabilities.Remove(applicability);

    public void AddEmployee(Employee employee) => Employees.Add(employee);

    public void AddEmployment(Employment employment) => Employments.Add(employment);

    public void AddAssignment(Assignment assignment) => Assignments.Add(assignment);

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

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
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

        return Task.CompletedTask;
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
    public EndEmploymentUseCase EndEmployment { get; }
    public ActiveWorkforceQuery ActiveWorkforce { get; }
    public EmployeeHistoryQuery History { get; }
    public HrEmployeeDirectoryQuery HrDirectory { get; }
    public HrEmployeeCardQuery HrCard { get; }
    public EmployeePhotoUseCases Photos { get; }
    public InMemoryEmployeePhotoStorage PhotoStorage { get; } = new();

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
        AddApplicability(DepartmentId, PositionId);
        AddApplicability(OtherDepartmentId, PositionId);
        AddApplicability(OtherDepartmentId, OtherPositionId);

        Hire = new HireEmployeeUseCase(Store, Clock, Workplace);
        HireWithProfile = new HireEmployeeWithProfileUseCase(Store, Clock, Workplace);
        UpdateProfile = new UpdateEmployeeHrProfileUseCase(Store, Clock, Workplace);
        Transfer = new TransferEmployeeUseCase(Store, Clock, Workplace);
        EndEmployment = new EndEmploymentUseCase(Store, Workplace);
        ActiveWorkforce = new ActiveWorkforceQuery(Store, Clock, Workplace);
        History = new EmployeeHistoryQuery(Store, Clock, Workplace);
        HrDirectory = new HrEmployeeDirectoryQuery(Store, Clock, Workplace);
        HrCard = new HrEmployeeCardQuery(Store, Clock, Workplace);
        Photos = new EmployeePhotoUseCases(Store, Workplace, PhotoStorage);
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
        bool canWriteSensitive = true) =>
        new(
            "Ayşe",
            "Yılmaz",
            startDate ?? Clock.Today,
            departmentId ?? DepartmentId,
            positionId ?? PositionId,
            profile ?? EmptyProfile(),
            canWriteSensitive);

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
            null, null, null, null, null, null, null, null, []);

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
