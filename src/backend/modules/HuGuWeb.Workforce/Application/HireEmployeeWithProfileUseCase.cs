using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class HireEmployeeWithProfileUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<HiredEmployee>> ExecuteAsync(
        HireEmployeeWithProfileCommand command,
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

        var today = clock.Today;
        var employeeId = Guid.CreateVersion7();
        var workType = command.WorkforceTerms?.WorkType ?? WorkType.FullTime;
        var employment = Employment.Open(
            Guid.CreateVersion7(),
            employeeId,
            command.EmploymentStartDate,
            today,
            workType);
        if (!employment.TryApplySeniorityStartDate(command.SeniorityStartDate, out var seniorityField, out var seniorityCode))
        {
            return WorkforceError.InvalidFields(
                seniorityCode ?? HrValidation.Codes.SeniorityStartDateInvalid,
                "Seniority start date is invalid.",
                seniorityField ?? HrValidation.Fields.SeniorityStartDate,
                seniorityCode ?? HrValidation.Codes.SeniorityStartDateInvalid);
        }

        var workforce = await EmploymentWorkforceComposer.ApplyAsync(
            store,
            employment,
            command.WorkforceTerms ?? EmploymentWorkforceWriteModel.Empty,
            workplace.Value.Organization.Id,
            cancellationToken);
        if (!workforce.IsSuccess)
        {
            return workforce.Error!;
        }

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

        var personnelNumber = await store.AllocatePersonnelNumberAsync(
            workplace.Value.Organization.Id,
            cancellationToken);
        if (!Employee.TryCreate(
                employeeId,
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

        var profile = await HrProfileComposer.ApplyAsync(
            store,
            employee,
            command.Profile,
            today,
            command.CanWriteSensitive,
            cancellationToken);
        if (!profile.IsSuccess)
        {
            return profile.Error!;
        }

        var certificates = await CertificatesComposer.ReplaceAllAsync(
            store,
            employee.Id,
            command.Certificates ?? [],
            clock.UtcNow,
            cancellationToken);
        if (!certificates.IsSuccess)
        {
            return certificates.Error!;
        }

        store.AddEmployee(employee);
        store.AddEmployment(employment);
        store.AddAssignment(assignment);

        var official = await OfficialEmploymentComposer.ApplyAsync(
            store,
            employment,
            command.OfficialProfile ?? OfficialEmploymentWriteModel.Empty,
            today,
            createIfEmpty: false,
            cancellationToken,
            [assignment]);
        if (!official.IsSuccess)
        {
            return official.Error!;
        }

        var bes = EmploymentBesComposer.Apply(
            store,
            employment,
            existing: null,
            command.BesSettings ?? EmploymentBesWriteModel.Empty,
            createIfEmpty: false);
        if (!bes.IsSuccess)
        {
            return bes.Error!;
        }

        try
        {
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (PersonnelNumberConflictException)
        {
            return WorkforceError.PersonnelNumberInUse();
        }
        catch (NationalIdentityConflictException)
        {
            return WorkforceError.NationalIdentityInUse();
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

public sealed record HireEmployeeWithProfileCommand(
    string GivenName,
    string FamilyName,
    DateOnly EmploymentStartDate,
    Guid DepartmentId,
    Guid PositionId,
    HrProfileWriteModel Profile,
    bool CanWriteSensitive,
    OfficialEmploymentWriteModel? OfficialProfile = null,
    EmploymentWorkforceWriteModel? WorkforceTerms = null,
    EmploymentBesWriteModel? BesSettings = null,
    DateOnly? SeniorityStartDate = null,
    IReadOnlyList<EmployeeCertificateDraft>? Certificates = null);

public sealed class NationalIdentityConflictException : Exception
{
    public NationalIdentityConflictException()
        : base("National identity is already in use.")
    {
    }
}
