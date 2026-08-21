using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class HireEmployeeUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<HiredEmployee>> ExecuteAsync(
        HireEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        if (!Employee.TryCreate(
                Guid.CreateVersion7(),
                workplace.Value.Organization.Id,
                command.GivenName,
                command.FamilyName,
                command.PersonnelNumber,
                out var employee,
                out var employeeError)
            || employee is null)
        {
            return WorkforceError.InvalidRequest("invalid-employee", employeeError ?? "Employee is invalid.");
        }

        var existing = await store.FindEmployeeByPersonnelNumberAsync(
            employee.OrganizationId,
            employee.PersonnelNumber,
            cancellationToken);
        if (existing is not null)
        {
            return WorkforceError.PersonnelNumberInUse();
        }

        var department = await store.GetDepartmentAsync(command.DepartmentId, cancellationToken);
        if (department is null || department.PropertyId != workplace.Value.Property.Id)
        {
            return WorkforceError.DepartmentNotFound();
        }

        var position = await store.GetPositionAsync(command.PositionId, cancellationToken);
        if (position is null || position.PropertyId != workplace.Value.Property.Id)
        {
            return WorkforceError.PositionNotFound();
        }

        var destination = AssignmentDestination.Ensure(department, position);
        if (!destination.IsSuccess)
        {
            return destination.Error!;
        }

        var today = clock.Today;
        var employment = Employment.Open(Guid.CreateVersion7(), employee.Id, command.EmploymentStartDate, today);
        var assignment = Assignment.StartPrimary(
            Guid.CreateVersion7(),
            employment.Id,
            department.Id,
            position.Id,
            command.EmploymentStartDate);

        if (!employment.TryEnsureAssignmentFits(assignment.Period, out _))
        {
            return WorkforceError.AssignmentOutsideEmployment();
        }

        store.AddEmployee(employee);
        store.AddEmployment(employment);
        store.AddAssignment(assignment);

        try
        {
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (PersonnelNumberConflictException)
        {
            return WorkforceError.PersonnelNumberInUse();
        }

        return new HiredEmployee(
            employee.Id,
            employee.PersonnelNumber,
            employee.GivenName,
            employee.FamilyName,
            employment.Id,
            employment.StartDate,
            employment.Status,
            assignment.Id,
            department.Id,
            position.Id);
    }
}

public sealed record HireEmployeeCommand(
    string GivenName,
    string FamilyName,
    string PersonnelNumber,
    DateOnly EmploymentStartDate,
    Guid DepartmentId,
    Guid PositionId);

public sealed record HiredEmployee(
    Guid EmployeeId,
    string PersonnelNumber,
    string GivenName,
    string FamilyName,
    Guid EmploymentId,
    DateOnly EmploymentStartDate,
    EmploymentStatus EmploymentStatus,
    Guid AssignmentId,
    Guid DepartmentId,
    Guid PositionId);

public sealed class PersonnelNumberConflictException : Exception
{
    public PersonnelNumberConflictException()
        : base("Personnel number is already in use.")
    {
    }
}
