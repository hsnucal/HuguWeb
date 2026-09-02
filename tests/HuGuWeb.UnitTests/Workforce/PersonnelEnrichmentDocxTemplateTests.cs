using System.IO.Compression;
using System.Text;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public sealed class PersonnelEnrichmentDocxTemplateTests
{
    [Fact]
    public void Embedded_Taslak_docx_asset_exists_and_is_valid_openxml_zip()
    {
        using var stream = HrDocumentDocxRenderer.OpenTemplateStream(
            HrDocumentTemplateDefaults.OvertimeConsentAssetPath);
        Assert.True(stream.Length > 0);

        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        copy.Position = 0;
        using var zip = new ZipArchive(copy, ZipArchiveMode.Read);
        Assert.Contains(
            zip.Entries,
            entry => string.Equals(
                HrDocumentDocxRenderer.NormalizeZipEntryPath(entry.FullName),
                "word/document.xml",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Embedded_Taslak_docx_contains_imported_legal_title_and_form_labels()
    {
        var plainText = HrDocumentDocxRenderer.ReadDocumentPlainText(
            HrDocumentTemplateDefaults.OvertimeConsentAssetPath);

        Assert.Contains("Muvafakat Belgesi", plainText, StringComparison.Ordinal);
        Assert.Contains("4857 sayılı", plainText, StringComparison.Ordinal);
        Assert.Contains("Giriş Tarihi:", plainText, StringComparison.Ordinal);
        Assert.Contains("İşçi Adı Soyadı:", plainText, StringComparison.Ordinal);
        Assert.Contains("İmza:", plainText, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\Users\\hsnuc", plainText, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPreviewHtml_uses_imported_docx_and_fills_draft_values()
    {
        var values = new Dictionary<string, string>
        {
            ["{{Employee.FullName}}"] = "Hasan Uçal",
            ["{{Employment.StartDate}}"] = "02.09.2026",
        };

        var html = HrDocumentDocxRenderer.BuildPreviewHtml(
            HrDocumentTemplateDefaults.OvertimeConsentAssetPath,
            values);

        Assert.Contains("Muvafakat Belgesi", html, StringComparison.Ordinal);
        Assert.Contains("Hasan Uçal", html, StringComparison.Ordinal);
        Assert.Contains("02.09.2026", html, StringComparison.Ordinal);
        Assert.Contains("İşçi Adı Soyadı:", html, StringComparison.Ordinal);
        Assert.Contains("İmza:", html, StringComparison.Ordinal);
        Assert.DoesNotContain("{{Employee.FullName}}", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_fills_allow_listed_placeholders_and_keeps_signature_blank()
    {
        var values = new Dictionary<string, string>
        {
            ["{{Employee.FullName}}"] = "Ali Tekin",
            ["{{Employment.StartDate}}"] = "01.09.2026",
        };

        var bytes = HrDocumentDocxRenderer.Render(HrDocumentTemplateDefaults.OvertimeConsentAssetPath, values);
        using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        var document = zip.Entries.First(entry =>
            string.Equals(
                HrDocumentDocxRenderer.NormalizeZipEntryPath(entry.FullName),
                "word/document.xml",
                StringComparison.OrdinalIgnoreCase));
        using var reader = new StreamReader(document.Open(), Encoding.UTF8);
        var xml = reader.ReadToEnd();

        Assert.Contains("Ali Tekin", xml, StringComparison.Ordinal);
        Assert.Contains("01.09.2026", xml, StringComparison.Ordinal);
        Assert.Contains("İşçi Adı Soyadı:", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("{{Employee.FullName}}", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("{{Employment.StartDate}}", xml, StringComparison.Ordinal);
        Assert.Contains("İmza:", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\Users\\hsnuc", xml, StringComparison.Ordinal);
    }

    [Fact]
    public void SanitizeFileName_removes_invalid_characters()
    {
        Assert.Equal(
            "Fazla_Calisma_Muvafakat_Ali_Tekin",
            HrDocumentDocxRenderer.SanitizeFileName("Fazla Çalışma Muvafakat/Ali:Tekin"));
    }

    [Fact]
    public void NormalizeAssetPath_rejects_traversal_and_absolute_paths()
    {
        Assert.Throws<ArgumentException>(() =>
            HrDocumentTemplate.NormalizeAssetPath(@"..\..\Windows\system32"));
        Assert.Throws<ArgumentException>(() =>
            HrDocumentTemplate.NormalizeAssetPath(@"C:\Users\hsnuc\Desktop\HuGuWeb\Taslak.docx"));
        Assert.Equal(
            "Templates/Onboarding/Taslak.docx",
            HrDocumentTemplate.NormalizeAssetPath("Templates\\Onboarding\\Taslak.docx"));
    }
}
