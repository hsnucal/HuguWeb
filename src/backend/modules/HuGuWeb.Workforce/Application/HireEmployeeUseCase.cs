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

        var applicable = await store.IsPositionApplicableToDepartmentAsync(
            department.Id,
            position.Id,
            cancellationToken);
        var destination = AssignmentDestination.Ensure(department, position, applicable);
        if (!destination.IsSuccess)
        {
            return destination.Error!;
        }

        var personnelNumber = await store.AllocatePersonnelNumberAsync(
            workplace.Value.Organization.Id,
            cancellationToken);
        if (!Employee.TryCreate(
                Guid.CreateVersion7(),
                workplace.Value.Organization.Id,
                command.GivenName,
                command.FamilyName,
                personnelNumber,
                out var employee,
                out var employeeError)
            || employee is null)
        {
            return WorkforceError.InvalidFields(
                "invalid-employee",
                employeeError ?? "Employee is invalid.",
                WorkforceError.FieldForEmployeeCode(employeeError),
                employeeError ?? "invalid-employee");
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
