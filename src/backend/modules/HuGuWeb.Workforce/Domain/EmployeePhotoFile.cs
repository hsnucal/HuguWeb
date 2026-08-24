using System.Text.RegularExpressions;

namespace HuGuWeb.Workforce.Domain;

public static class EmployeePhotoFile
{
    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypes = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/jpg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private static readonly Regex StorageKeyPattern = new(
        "^[a-f0-9]{32}\\.(jpg|png|webp)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static bool TryValidate(
        Stream content,
        string? declaredContentType,
        long? declaredLength,
        out byte[] bytes,
        out string contentType,
        out string extension,
        out string? error)
    {
        bytes = [];
        contentType = string.Empty;
        extension = string.Empty;
        error = null;

        if (declaredLength is > EmployeePhoto.MaxBytes)
        {
            error = "Photo exceeds the maximum size.";
            return false;
        }

        var declaredType = declaredContentType?.Split(';', 2)[0].Trim();
        if (string.IsNullOrWhiteSpace(declaredType)
            || !AllowedContentTypes.TryGetValue(declaredType, out var mappedExtension)
            || mappedExtension is null)
        {
            error = "Photo type is not allowed.";
            return false;
        }

        extension = mappedExtension;

        using var buffer = new MemoryStream();
        var copy = new byte[16 * 1024];
        int read;
        while ((read = content.Read(copy, 0, copy.Length)) > 0)
        {
            if (buffer.Length + read > EmployeePhoto.MaxBytes)
            {
                error = "Photo exceeds the maximum size.";
                return false;
            }

            buffer.Write(copy, 0, read);
        }

        if (buffer.Length == 0)
        {
            error = "Photo is empty.";
            return false;
        }

        bytes = buffer.ToArray();
        if (!MatchesSignature(bytes, extension))
        {
            error = "Photo type is not allowed.";
            bytes = [];
            return false;
        }

        contentType = extension switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
        return true;
    }

    public static string CreateStorageKey(string extension) =>
        $"{Guid.CreateVersion7().ToString("N")}{extension}";

    public static bool IsSafeStorageKey(string? storageKey) =>
        !string.IsNullOrWhiteSpace(storageKey) && StorageKeyPattern.IsMatch(storageKey);

    private static bool MatchesSignature(byte[] bytes, string extension)
    {
        if (extension == ".jpg")
        {
            return bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        }

        if (extension == ".png")
        {
            return bytes.Length >= 8
                && bytes[0] == 0x89
                && bytes[1] == 0x50
                && bytes[2] == 0x4E
                && bytes[3] == 0x47;
        }

        if (extension == ".webp")
        {
            return bytes.Length >= 12
                && bytes[0] == (byte)'R'
                && bytes[1] == (byte)'I'
                && bytes[2] == (byte)'F'
                && bytes[3] == (byte)'F'
                && bytes[8] == (byte)'W'
                && bytes[9] == (byte)'E'
                && bytes[10] == (byte)'B'
                && bytes[11] == (byte)'P';
        }

        return false;
    }
}
