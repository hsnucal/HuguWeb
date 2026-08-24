using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using Microsoft.Extensions.Options;

namespace HuGuWeb.Workforce.Infrastructure;

public sealed class EmployeePhotoStorageOptions
{
    public const string SectionName = "Workforce:EmployeePhotos";

    public string RootPath { get; set; } = "App_Data/employee-photos";
}

public sealed class FileSystemEmployeePhotoStorage : IEmployeePhotoStorage
{
    private readonly string _root;

    public FileSystemEmployeePhotoStorage(IOptions<EmployeePhotoStorageOptions> options)
    {
        var configured = string.IsNullOrWhiteSpace(options.Value.RootPath)
            ? "App_Data/employee-photos"
            : options.Value.RootPath.Trim();
        _root = Path.GetFullPath(
            Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(Directory.GetCurrentDirectory(), configured));
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(string storageKey, byte[] content, CancellationToken cancellationToken)
    {
        var path = ResolvePath(storageKey);
        await File.WriteAllBytesAsync(path, content, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        if (!EmployeePhotoFile.IsSafeStorageKey(storageKey))
        {
            throw new InvalidOperationException("Photo storage key is invalid.");
        }

        var combined = Path.GetFullPath(Path.Combine(_root, storageKey));
        var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;
        if (!combined.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Photo storage key is invalid.");
        }

        return combined;
    }
}
