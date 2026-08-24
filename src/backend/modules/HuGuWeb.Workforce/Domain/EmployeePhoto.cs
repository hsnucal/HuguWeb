namespace HuGuWeb.Workforce.Domain;

public sealed class EmployeePhoto
{
    public const int StorageKeyMaxLength = 80;
    public const int ContentTypeMaxLength = 64;
    public const int MaxBytes = 2 * 1024 * 1024;

    private EmployeePhoto()
    {
        StorageKey = string.Empty;
        ContentType = string.Empty;
    }

    private EmployeePhoto(
        Guid id,
        Guid employeeId,
        string storageKey,
        string contentType,
        int byteSize,
        DateTimeOffset uploadedAtUtc)
    {
        Id = id;
        EmployeeId = employeeId;
        StorageKey = storageKey;
        ContentType = contentType;
        ByteSize = byteSize;
        UploadedAtUtc = uploadedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string StorageKey { get; private set; }
    public string ContentType { get; private set; }
    public int ByteSize { get; private set; }
    public DateTimeOffset UploadedAtUtc { get; private set; }

    public static EmployeePhoto Create(
        Guid id,
        Guid employeeId,
        string storageKey,
        string contentType,
        int byteSize,
        DateTimeOffset uploadedAtUtc) =>
        new(id, employeeId, storageKey, contentType, byteSize, uploadedAtUtc);

    public void Replace(string storageKey, string contentType, int byteSize, DateTimeOffset uploadedAtUtc)
    {
        StorageKey = storageKey;
        ContentType = contentType;
        ByteSize = byteSize;
        UploadedAtUtc = uploadedAtUtc;
    }
}
