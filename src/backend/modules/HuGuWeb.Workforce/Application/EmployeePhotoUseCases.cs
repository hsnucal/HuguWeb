using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class EmployeePhotoUseCases(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext,
    IEmployeePhotoStorage photoStorage,
    IWorkforceClock clock)
{
    public async Task<WorkforceResult<EmployeePhoto>> UploadAsync(
        Guid employeeId,
        Stream content,
        string? declaredContentType,
        long? declaredLength,
        CancellationToken cancellationToken)
    {
        var employee = await RequireEmployeeAsync(employeeId, cancellationToken);
        if (!employee.IsSuccess)
        {
            return employee.Error!;
        }

        if (!EmployeePhotoFile.TryValidate(
                content,
                declaredContentType,
                declaredLength,
                out var bytes,
                out var contentType,
                out var extension,
                out var error))
        {
            return WorkforceError.InvalidPhoto(error ?? "Photo is invalid.");
        }

        var storageKey = EmployeePhotoFile.CreateStorageKey(extension);
        await photoStorage.SaveAsync(storageKey, bytes, cancellationToken);

        var existing = await store.GetEmployeePhotoAsync(employee.Value!.Id, cancellationToken);
        var previousKey = existing?.StorageKey;
        if (existing is null)
        {
            store.AddEmployeePhoto(EmployeePhoto.Create(
                Guid.CreateVersion7(),
                employee.Value.Id,
                storageKey,
                contentType,
                bytes.Length,
                clock.UtcNow));
        }
        else
        {
            existing.Replace(storageKey, contentType, bytes.Length, clock.UtcNow);
        }

        try
        {
            await store.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await photoStorage.DeleteAsync(storageKey, cancellationToken);
            throw;
        }

        if (!string.IsNullOrEmpty(previousKey) && previousKey != storageKey)
        {
            await photoStorage.DeleteAsync(previousKey, cancellationToken);
        }

        var saved = await store.GetEmployeePhotoAsync(employee.Value.Id, cancellationToken);
        return saved!;
    }

    public async Task<WorkforceResult> RemoveAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var employee = await RequireEmployeeAsync(employeeId, cancellationToken);
        if (!employee.IsSuccess)
        {
            return employee.Error!;
        }

        var existing = await store.GetEmployeePhotoAsync(employee.Value!.Id, cancellationToken);
        if (existing is null)
        {
            return WorkforceError.PhotoNotFound();
        }

        var storageKey = existing.StorageKey;
        store.RemoveEmployeePhoto(existing);
        await store.SaveChangesAsync(cancellationToken);
        await photoStorage.DeleteAsync(storageKey, cancellationToken);
        return WorkforceResult.Success();
    }

    public async Task<WorkforceResult<EmployeePhotoContent>> OpenAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await RequireEmployeeAsync(employeeId, cancellationToken);
        if (!employee.IsSuccess)
        {
            return employee.Error!;
        }

        var photo = await store.GetEmployeePhotoAsync(employee.Value!.Id, cancellationToken);
        if (photo is null || !EmployeePhotoFile.IsSafeStorageKey(photo.StorageKey))
        {
            return WorkforceError.PhotoNotFound();
        }

        var stream = await photoStorage.OpenReadAsync(photo.StorageKey, cancellationToken);
        if (stream is null)
        {
            return WorkforceError.PhotoNotFound();
        }

        return new EmployeePhotoContent(stream, photo.ContentType);
    }

    private async Task<WorkforceResult<Employee>> RequireEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.EmployeeNotFound();
        }

        return employee;
    }
}

public sealed record EmployeePhotoContent(Stream Content, string ContentType);
