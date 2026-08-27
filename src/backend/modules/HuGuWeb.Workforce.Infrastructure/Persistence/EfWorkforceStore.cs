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

    public async Task<IReadOnlyList<Property>> ListPropertiesAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        await dbContext.Properties
            .Where(entity => entity.OrganizationId == organizationId)
            .OrderBy(entity => entity.Name)
            .ToListAsync(cancellationToken);

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

    public async Task<IReadOnlyList<Department>> ListDepartmentsForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        await dbContext.Departments
            .Where(entity => dbContext.Properties.Any(property =>
                property.Id == entity.PropertyId && property.OrganizationId == organizationId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Position>> ListPositionsAsync(
        Guid propertyId,
        CancellationToken cancellationToken) =>
        await dbContext.Positions
            .Where(entity => entity.PropertyId == propertyId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Position>> ListPositionsForOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        await dbContext.Positions
            .Where(entity => dbContext.Properties.Any(property =>
                property.Id == entity.PropertyId && property.OrganizationId == organizationId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DepartmentPositionApplicability>> ListApplicabilitiesForPositionsAsync(
        IReadOnlyCollection<Guid> positionIds,
        CancellationToken cancellationToken)
    {
        if (positionIds.Count == 0)
        {
            return [];
        }

        return await dbContext.DepartmentPositionApplicabilities
            .Where(entity => positionIds.Contains(entity.PositionId))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsPositionApplicableToDepartmentAsync(
        Guid departmentId,
        Guid positionId,
        CancellationToken cancellationToken) =>
        dbContext.DepartmentPositionApplicabilities.AnyAsync(
            entity => entity.DepartmentId == departmentId && entity.PositionId == positionId,
            cancellationToken);

    public async Task<string> AllocatePersonnelNumberAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "PersonnelNumberSequences" ("OrganizationId", "NextValue")
            VALUES ({organizationId}, {PersonnelNumberSequence.StartingValue})
            ON CONFLICT ("OrganizationId") DO NOTHING
            """,
            cancellationToken);

        while (true)
        {
            var reserved = await dbContext.Database
                .SqlQuery<int>($"""
                    UPDATE "PersonnelNumberSequences"
                    SET "NextValue" = "NextValue" + 1
                    WHERE "OrganizationId" = {organizationId}
                    RETURNING "NextValue" - 1 AS "Value"
                    """)
                .ToListAsync(cancellationToken);
            if (reserved.Count != 1)
            {
                throw new InvalidOperationException("Personnel number sequence is missing.");
            }

            var formatted = PersonnelNumber.Format(reserved[0]);
            var taken = await dbContext.Employees.AnyAsync(
                entity => entity.OrganizationId == organizationId && entity.PersonnelNumber == formatted,
                cancellationToken);
            if (!taken)
            {
                return formatted;
            }
        }
    }

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

    public void AddApplicability(DepartmentPositionApplicability applicability) =>
        dbContext.DepartmentPositionApplicabilities.Add(applicability);

    public void RemoveApplicability(DepartmentPositionApplicability applicability) =>
        dbContext.DepartmentPositionApplicabilities.Remove(applicability);

    public void AddEmployee(Employee employee) => dbContext.Employees.Add(employee);

    public void AddEmployment(Employment employment) => dbContext.Employments.Add(employment);

    public void AddAssignment(Assignment assignment) => dbContext.Assignments.Add(assignment);

    public Task<EmployeeHrProfile?> GetHrProfileAsync(Guid employeeId, CancellationToken cancellationToken) =>
        dbContext.EmployeeHrProfiles.FirstOrDefaultAsync(entity => entity.EmployeeId == employeeId, cancellationToken);

    public async Task<IReadOnlyList<EmployeeHrProfile>> ListHrProfilesForEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
        {
            return [];
        }

        return await dbContext.EmployeeHrProfiles
            .Where(entity => employeeIds.Contains(entity.EmployeeId))
            .ToListAsync(cancellationToken);
    }

    public Task<EmployeeHrProfile?> FindHrProfileByNationalIdentityAsync(
        Guid organizationId,
        NationalIdentityScheme scheme,
        string normalizedNationalIdentityNumber,
        CancellationToken cancellationToken) =>
        dbContext.EmployeeHrProfiles.FirstOrDefaultAsync(
            entity => entity.OrganizationId == organizationId
                && entity.NationalIdentityScheme == scheme
                && entity.NormalizedNationalIdentityNumber == normalizedNationalIdentityNumber,
            cancellationToken);

    public async Task<IReadOnlyList<EmergencyContact>> ListEmergencyContactsAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        await dbContext.EmergencyContacts
            .Where(entity => entity.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EmergencyContact>> ListEmergencyContactsForEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
        {
            return [];
        }

        return await dbContext.EmergencyContacts
            .Where(entity => employeeIds.Contains(entity.EmployeeId))
            .ToListAsync(cancellationToken);
    }

    public Task<EmployeePhoto?> GetEmployeePhotoAsync(Guid employeeId, CancellationToken cancellationToken) =>
        dbContext.EmployeePhotos.FirstOrDefaultAsync(entity => entity.EmployeeId == employeeId, cancellationToken);

    public async Task<IReadOnlyList<EmployeePhoto>> ListEmployeePhotosForEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
        {
            return [];
        }

        return await dbContext.EmployeePhotos
            .Where(entity => employeeIds.Contains(entity.EmployeeId))
            .ToListAsync(cancellationToken);
    }

    public void AddHrProfile(EmployeeHrProfile profile) => dbContext.EmployeeHrProfiles.Add(profile);

    public void AddEmergencyContact(EmergencyContact contact) => dbContext.EmergencyContacts.Add(contact);

    public void RemoveEmergencyContact(EmergencyContact contact) => dbContext.EmergencyContacts.Remove(contact);

    public void AddEmployeePhoto(EmployeePhoto photo) => dbContext.EmployeePhotos.Add(photo);

    public void RemoveEmployeePhoto(EmployeePhoto photo) => dbContext.EmployeePhotos.Remove(photo);

    public Task<SgkWorkplaceRegistration?> GetSgkWorkplaceRegistrationAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        dbContext.SgkWorkplaceRegistrations.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SgkWorkplaceRegistration>> ListSgkWorkplaceRegistrationsAsync(
        Guid propertyId,
        CancellationToken cancellationToken) =>
        await dbContext.SgkWorkplaceRegistrations
            .Where(entity => entity.PropertyId == propertyId)
            .ToListAsync(cancellationToken);

    public void AddSgkWorkplaceRegistration(SgkWorkplaceRegistration registration) =>
        dbContext.SgkWorkplaceRegistrations.Add(registration);

    public Task<OfficialEmploymentProfile?> GetOfficialEmploymentProfileAsync(
        Guid employmentId,
        CancellationToken cancellationToken) =>
        dbContext.OfficialEmploymentProfiles.FirstOrDefaultAsync(
            entity => entity.EmploymentId == employmentId,
            cancellationToken);

    public async Task<IReadOnlyList<OfficialEmploymentProfile>> ListOfficialEmploymentProfilesForEmploymentsAsync(
        IReadOnlyCollection<Guid> employmentIds,
        CancellationToken cancellationToken)
    {
        if (employmentIds.Count == 0)
        {
            return [];
        }

        return await dbContext.OfficialEmploymentProfiles
            .Where(entity => employmentIds.Contains(entity.EmploymentId))
            .ToListAsync(cancellationToken);
    }

    public void AddOfficialEmploymentProfile(OfficialEmploymentProfile profile) =>
        dbContext.OfficialEmploymentProfiles.Add(profile);

    public Task<SgkDocumentType?> GetSgkDocumentTypeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.SgkDocumentTypes.FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);

    public async Task<IReadOnlyList<SgkDocumentType>> ListSgkDocumentTypesAsync(CancellationToken cancellationToken) =>
        await dbContext.SgkDocumentTypes.OrderBy(entity => entity.Code).ToListAsync(cancellationToken);

    public Task<ApplicableLawCode?> GetApplicableLawCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.ApplicableLawCodes.FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);

    public async Task<IReadOnlyList<ApplicableLawCode>> ListApplicableLawCodesAsync(
        CancellationToken cancellationToken) =>
        await dbContext.ApplicableLawCodes.OrderBy(entity => entity.Code).ToListAsync(cancellationToken);

    public Task<InsuranceBranch?> GetInsuranceBranchAsync(string code, CancellationToken cancellationToken) =>
        dbContext.InsuranceBranches.FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);

    public async Task<IReadOnlyList<InsuranceBranch>> ListInsuranceBranchesAsync(
        CancellationToken cancellationToken) =>
        await dbContext.InsuranceBranches.OrderBy(entity => entity.Code).ToListAsync(cancellationToken);

    public Task<SgkOccupationCode?> GetSgkOccupationCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.SgkOccupationCodes.FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);

    public async Task<IReadOnlyList<SgkOccupationCode>> SearchSgkOccupationCodesAsync(
        string? query,
        int take,
        CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(take, 1, OfficialLookupsQuery.OccupationSearchLimit);
        var source = dbContext.SgkOccupationCodes.Where(entity => entity.IsActive);
        var term = query?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            var pattern = $"%{EscapeLike(term)}%";
            source = source.Where(entity =>
                EF.Functions.ILike(entity.Code, pattern)
                || EF.Functions.ILike(entity.Description, pattern));
        }

        return await source
            .OrderBy(entity => entity.Code)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public Task<EmploymentDutyCode?> GetEmploymentDutyCodeAsync(string code, CancellationToken cancellationToken) =>
        dbContext.EmploymentDutyCodes.FirstOrDefaultAsync(entity => entity.Code == code, cancellationToken);

    public async Task<IReadOnlyList<EmploymentDutyCode>> ListEmploymentDutyCodesAsync(
        CancellationToken cancellationToken) =>
        await dbContext.EmploymentDutyCodes.OrderBy(entity => entity.Code).ToListAsync(cancellationToken);

    public Task<EmploymentBesSettings?> GetEmploymentBesSettingsAsync(
        Guid employmentId,
        CancellationToken cancellationToken) =>
        dbContext.EmploymentBesSettings.FirstOrDefaultAsync(
            entity => entity.EmploymentId == employmentId,
            cancellationToken);

    public void AddEmploymentBesSettings(EmploymentBesSettings settings) =>
        dbContext.EmploymentBesSettings.Add(settings);

    public Task<EmployeePaymentProfile?> GetPaymentProfileAsync(Guid employeeId, CancellationToken cancellationToken) =>
        dbContext.EmployeePaymentProfiles.FirstOrDefaultAsync(entity => entity.EmployeeId == employeeId, cancellationToken);

    public void AddPaymentProfile(EmployeePaymentProfile profile) =>
        dbContext.EmployeePaymentProfiles.Add(profile);

    public async Task<IReadOnlyList<PersonnelProfileChange>> ListPersonnelProfileChangesAsync(
        Guid employeeId,
        CancellationToken cancellationToken) =>
        await dbContext.PersonnelProfileChanges
            .Where(entity => entity.EmployeeId == employeeId)
            .OrderByDescending(entity => entity.ChangedAtUtc)
            .ToListAsync(cancellationToken);

    public void AddPersonnelProfileChange(PersonnelProfileChange change) =>
        dbContext.PersonnelProfileChanges.Add(change);

    public void AddPersonnelImportRun(PersonnelImportRun importRun) =>
        dbContext.PersonnelImportRuns.Add(importRun);

    public async Task<IWorkforceTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfWorkforceTransaction(transaction);
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

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
        catch (DbUpdateException exception) when (IsNationalIdentityConflict(exception))
        {
            throw new NationalIdentityConflictException();
        }
    }

    private static bool IsPersonnelNumberConflict(DbUpdateException exception) =>
        IsUniqueViolation(exception, WorkforceDbContext.PersonnelNumberIndexName);

    private static bool IsNationalIdentityConflict(DbUpdateException exception) =>
        IsUniqueViolation(exception, WorkforceDbContext.NationalIdentityIndexName);

    private static bool IsUniqueViolation(DbUpdateException exception, string constraintName)
    {
        for (var current = exception.InnerException; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation
                && string.Equals(postgres.ConstraintName, constraintName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

internal sealed class EfWorkforceTransaction(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
    : IWorkforceTransaction
{
    public async Task CommitAsync(CancellationToken cancellationToken) =>
        await transaction.CommitAsync(cancellationToken);

    public async Task RollbackAsync(CancellationToken cancellationToken) =>
        await transaction.RollbackAsync(cancellationToken);

    public async ValueTask DisposeAsync() => await transaction.DisposeAsync();
}
