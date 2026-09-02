using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class PersonnelEnrichmentDocumentTemplateTests
{
    [Fact]
    public async Task EnsureDefaults_SeedsOvertimeConsentWithoutInventedLegalBody()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);

        var template = Assert.Single(harness.Store.HrDocumentTemplates);
        Assert.Equal(HrDocumentTemplateDefaults.OvertimeConsentCode, template.Code);
        Assert.Equal(HrDocumentTemplateDefaults.OvertimeConsentAssetPath, template.TemplateAssetPath);
        Assert.Contains("{{Employee.FullName}}", template.Content, StringComparison.Ordinal);
        Assert.Contains("{{Employment.StartDate}}", template.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("[Body pending PO Taslak.docx import]", template.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void Renderer_RejectsUnknownPlaceholder()
    {
        Assert.False(HrDocumentTemplateRenderer.TryRender(
            "Hello {{Employee.Secret}}",
            new HrDocumentRenderContext(
                "Ayşe Yılmaz",
                "Ayşe",
                "Yılmaz",
                "P-1",
                new DateOnly(1990, 1, 2),
                new DateOnly(2026, 8, 21),
                "Kat",
                "Görevli",
                "Org",
                "Prop",
                new DateOnly(2026, 8, 21)),
            culture: null,
            out _,
            out var field,
            out var code));
        Assert.Equal(HrValidation.Fields.DocumentTemplatePlaceholder, field);
        Assert.Equal(HrValidation.Codes.DocumentTemplateUnknownPlaceholder, code);
    }

    [Fact]
    public void Renderer_FormatsDates_AndStripsScript()
    {
        Assert.True(HrDocumentTemplateRenderer.TryRender(
            "<p onclick=\"alert(1)\">{{Employee.FullName}} {{Employment.StartDate}}<script>x()</script></p>",
            new HrDocumentRenderContext(
                "Ayşe Yılmaz",
                "Ayşe",
                "Yılmaz",
                "P-1",
                null,
                new DateOnly(2026, 8, 21),
                "Kat",
                "Görevli",
                "Org",
                "Prop",
                new DateOnly(2026, 9, 1)),
            culture: null,
            out var rendered,
            out _,
            out _));

        Assert.Contains("Ayşe Yılmaz", rendered, StringComparison.Ordinal);
        Assert.Contains("21.08.2026", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("script", rendered, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", rendered, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Preview_ResolvesServerSideEmployeeData()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);
        var template = harness.Store.HrDocumentTemplates[0];

        var preview = await harness.PreviewHrDocumentTemplate.ExecuteAsync(
            hired.Value!.EmployeeId,
            template.Id,
            CancellationToken.None);

        Assert.True(preview.IsSuccess, preview.Error?.Detail);
        Assert.Contains("Ayşe Yılmaz", preview.Value!.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("21.08.2026", preview.Value.RenderedContent, StringComparison.Ordinal);
        Assert.Contains("İmza", preview.Value.RenderedContent, StringComparison.Ordinal);

        var listed = await harness.HrDocumentTemplates.ListActiveAsync(
            HrDocumentTemplateCategory.Onboarding,
            CancellationToken.None);
        Assert.True(listed.IsSuccess);
        Assert.Single(listed.Value!);
        Assert.True(listed.Value![0].HasDocxAsset);
    }
}
