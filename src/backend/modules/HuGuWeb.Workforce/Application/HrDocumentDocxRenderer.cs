using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// Resolves application-owned DOCX template assets and fills allow-listed placeholders.
/// No Office Interop; no client filesystem paths.
/// </summary>
public static class HrDocumentDocxRenderer
{
    public const string OvertimeConsentLogicalName = "HuGuWeb.Workforce.Templates.Onboarding.Taslak.docx";

    private static readonly Regex PlaceholderPattern = new(
        @"\{\{[A-Za-z0-9_.]+\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static Stream OpenTemplateStream(string assetPath)
    {
        var normalized = HrDocumentTemplate.NormalizeAssetPath(assetPath)
            ?? throw new ArgumentException("Template asset path is required.", nameof(assetPath));

        if (!string.Equals(normalized, HrDocumentTemplateDefaults.OvertimeConsentAssetPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unsupported template asset '{normalized}'.");
        }

        var assembly = typeof(HrDocumentDocxRenderer).Assembly;
        var stream = assembly.GetManifestResourceStream(OvertimeConsentLogicalName);
        if (stream is null)
        {
            throw new FileNotFoundException(
                $"Embedded DOCX template '{OvertimeConsentLogicalName}' was not found.",
                OvertimeConsentLogicalName);
        }

        return stream;
    }

    public static byte[] Render(
        string assetPath,
        IReadOnlyDictionary<string, string> values)
    {
        using var source = OpenTemplateStream(assetPath);
        using var input = new MemoryStream();
        source.CopyTo(input);
        return RenderDocumentXml(input.ToArray(), values);
    }

    public static string BuildPreviewHtml(
        string assetPath,
        IReadOnlyDictionary<string, string> values)
    {
        var xml = ReplacePlaceholders(ReadDocumentXml(assetPath), values);
        return ConvertDocumentXmlToHtml(xml);
    }

    public static string ReadDocumentPlainText(string assetPath)
    {
        return ExtractPlainText(ReadDocumentXml(assetPath));
    }

    private static string ReadDocumentXml(string assetPath)
    {
        using var source = OpenTemplateStream(assetPath);
        using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: false);
        var entry = archive.Entries.First(item =>
            string.Equals(
                NormalizeZipEntryPath(item.FullName),
                "word/document.xml",
                StringComparison.OrdinalIgnoreCase));
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static byte[] RenderDocumentXml(byte[] bytes, IReadOnlyDictionary<string, string> values)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read, leaveOpen: false))
        using (var writer = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in archive.Entries)
            {
                var entryPath = NormalizeZipEntryPath(entry.FullName);
                var target = writer.CreateEntry(entryPath, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var targetStream = target.Open();
                if (string.Equals(entryPath, "word/document.xml", StringComparison.OrdinalIgnoreCase))
                {
                    using var reader = new StreamReader(entryStream, Encoding.UTF8);
                    var xml = ReplacePlaceholders(reader.ReadToEnd(), values);
                    var payload = Encoding.UTF8.GetBytes(xml);
                    targetStream.Write(payload, 0, payload.Length);
                }
                else
                {
                    entryStream.CopyTo(targetStream);
                }
            }
        }

        return output.ToArray();
    }

    private static string ConvertDocumentXmlToHtml(string xml)
    {
        XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var document = XDocument.Parse(xml);
        var builder = new StringBuilder();
        foreach (var paragraph in document.Descendants(word + "p"))
        {
            var line = string.Concat(paragraph.Descendants(word + "t").Select(node => node.Value)).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            builder.Append("<p>")
                .Append(SecurityElementEscape(line))
                .Append("</p>");
        }

        return builder.ToString();
    }

    private static string ExtractPlainText(string xml)
    {
        XNamespace word = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        var document = XDocument.Parse(xml);
        var builder = new StringBuilder();
        foreach (var paragraph in document.Descendants(word + "p"))
        {
            var line = string.Concat(paragraph.Descendants(word + "t").Select(node => node.Value)).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(line);
        }

        return builder.ToString();
    }

    public static string NormalizeZipEntryPath(string path) =>
        path.Replace('\\', '/');

    public static string ReplacePlaceholders(string xml, IReadOnlyDictionary<string, string> values)
    {
        return PlaceholderPattern.Replace(xml, match =>
        {
            var token = match.Value;
            return values.TryGetValue(token, out var replacement)
                ? SecurityElementEscape(replacement)
                : token;
        });
    }

    public static string SanitizeFileName(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return "document";
        }

        var buffer = new StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            var ascii = FoldTurkishAscii(ch);
            if (ascii is '"' or '<' or '>' or '|' or ':' or '*' or '?' or '\\' or '/' or '\0')
            {
                buffer.Append('_');
            }
            else if (char.IsWhiteSpace(ascii))
            {
                buffer.Append('_');
            }
            else
            {
                buffer.Append(ascii);
            }
        }

        var result = buffer.ToString().Trim('_');
        return result.Length == 0 ? "document" : result;
    }

    private static char FoldTurkishAscii(char ch) => ch switch
    {
        'ç' => 'c',
        'Ç' => 'C',
        'ğ' => 'g',
        'Ğ' => 'G',
        'ı' => 'i',
        'İ' => 'I',
        'ö' => 'o',
        'Ö' => 'O',
        'ş' => 's',
        'Ş' => 'S',
        'ü' => 'u',
        'Ü' => 'U',
        _ => ch,
    };

    public static string FormatTrDate(DateOnly date) =>
        date.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"));

    private static string SecurityElementEscape(string value)
    {
        return new XText(value).ToString();
    }
}
