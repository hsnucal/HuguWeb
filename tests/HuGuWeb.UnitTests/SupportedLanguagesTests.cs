using HuGuWeb.Api.Identity;

namespace HuGuWeb.UnitTests;

public class SupportedLanguagesTests
{
    [Theory]
    [InlineData("tr", "tr")]
    [InlineData("en", "en")]
    [InlineData("ru", "ru")]
    [InlineData("TR", "tr")]
    [InlineData(" En ", "en")]
    [InlineData("RU", "ru")]
    public void TryNormalize_AcceptsSupportedLanguageCodes(string input, string expected)
    {
        var accepted = SupportedLanguages.TryNormalize(input, out var normalized);

        Assert.True(accepted);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    [InlineData("ru-RU")]
    [InlineData("english")]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("zh")]
    [InlineData("tr_TR")]
    [InlineData("Türkçe")]
    public void TryNormalize_RejectsUnsupportedValues(string? input)
    {
        var accepted = SupportedLanguages.TryNormalize(input, out var normalized);

        Assert.False(accepted);
        Assert.Equal(SupportedLanguages.Default, normalized);
    }

    [Fact]
    public void IsSupported_AcceptsOnlyCanonicalLanguageCodes()
    {
        Assert.True(SupportedLanguages.IsSupported("tr"));
        Assert.True(SupportedLanguages.IsSupported("EN"));
        Assert.False(SupportedLanguages.IsSupported("en-US"));
        Assert.False(SupportedLanguages.IsSupported("xx"));
    }

    [Fact]
    public void Default_IsTurkish()
    {
        Assert.Equal("tr", SupportedLanguages.Default);
        Assert.Equal(["tr", "en", "ru"], SupportedLanguages.All);
    }
}
