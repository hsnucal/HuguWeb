using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HuGuWeb.Workforce.Infrastructure.Persistence;

public sealed class EfWorkforceStore(WorkforceDbContext dbContext) : IWorkforceStore
{
    public Task<Organization?> GetOrganizationAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Organizations.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public Task<Property?> GetPropertyAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Properties.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public Task<Department?> GetDepartmentAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Departments.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public Task<Position?> GetPositionAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Positions.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public Task<Employee?> GetEmployeeAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Employees.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public Task<Employee?> FindEmployeeByPersonnelNumberAsync(
        Guid organizationId,
        string personnelNumber,
        CancellationToken cancellationToken) =>
        dbContext.Employees.FirstOrDefaultAsync(
            entity => entity.OrganizationId == organizationId && entity.PersonnelNumber == personnelNumber,
            cancellationToken);

    public async Task<IReadOnlyList<Department>> ListDepartmentsAsync(
        Guid propertyId,
        CancellationToken cancellationToken) =>
        await dbContext.Departments
            .Where(entity => entity.PropertyId == propertyId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Position>> ListPositionsAsync(
        Guid propertyId,
        CancellationToken cancellationToken) =>
        await dbContext.Positions
            .Where(entity => entity.PropertyId == propertyId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Employee>> ListEmployeesAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        await dbContext.Employees
            .Where(entity => entity.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Employment>> ListEmploymentsAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        await dbContext.Employments
            .Where(entity => entity.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Employment>> ListEmploymentsForEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Employments
            .Where(entity => employeeIds.Contains(entity.EmployeeId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Assignment>> ListAssignmentsAsync(
        Guid employmentId,
        CancellationToken cancellationToken) =>
        await dbContext.Assignments
            .Where(entity => entity.EmploymentId == employmentId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Assignment>> ListAssignmentsForEmploymentsAsync(
        IReadOnlyCollection<Guid> employmentIds,
        CancellationToken cancellationToken)
    {
        if (employmentIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Assignments
            .Where(entity => employmentIds.Contains(entity.EmploymentId))
            .ToListAsync(cancellationToken);
    }

    public void AddDepartment(Department department) => dbContext.Departments.Add(department);

    public void AddPosition(Position position) => dbContext.Positions.Add(position);

    public void AddEmployee(Employee employee) => dbContext.Employees.Add(employee);

    public void AddEmployment(Employment employment) => dbContext.Employments.Add(employment);

    public void AddAssignment(Assignment assignment) => dbContext.Assignments.Add(assignment);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsPersonnelNumberConflict(exception))
        {
            throw new PersonnelNumberConflictException();
        }
    }

    private static bool IsPersonnelNumberConflict(DbUpdateException exception)
    {
        for (var current = exception.InnerException; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation
                && string.Equals(
                    postgres.ConstraintName,
                    WorkforceDbContext.PersonnelNumberIndexName,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
