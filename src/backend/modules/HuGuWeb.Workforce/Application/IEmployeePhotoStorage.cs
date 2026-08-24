namespace HuGuWeb.Workforce.Application;

public interface IEmployeePhotoStorage
{
    Task SaveAsync(string storageKey, byte[] content, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
