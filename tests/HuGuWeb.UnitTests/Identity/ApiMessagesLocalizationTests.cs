using System.Globalization;
using HuGuWeb.Api.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HuGuWeb.UnitTests.Identity;

public class ApiMessagesLocalizationTests
{
    [Theory]
    [InlineData("tr", "error.employee-not-found.detail", "Personel kaydı bulunamadı.")]
    [InlineData("en", "error.employee-not-found.detail", "The employee was not found.")]
    [InlineData("ru", "error.employee-not-found.detail", "Сотрудник не найден.")]
    [InlineData("tr", "error.sgk-workplace-inactive.detail", "Pasif bir SGK işyeri kaydı yeni seçilemez.")]
    [InlineData("tr", "error.leave-overlap.detail", "Bu izin, aynı iş ilişkisindeki başka bir kayıtlı izinle çakışıyor.")]
    [InlineData("en", "error.leave-overlap.detail", "This leave overlaps another recorded leave for the same employment.")]
    [InlineData("ru", "error.leave-overlap.detail", "Этот отпуск пересекается с другой записанной записью по тем же трудовым отношениям.")]
    public void ErrorCodes_HaveTrEnRuResources(string culture, string key, string expected)
    {
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        var factory = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions()),
            NullLoggerFactory.Instance);
        var localizer = new ApiErrorLocalizer(
            new StringLocalizer<CommonMessages>(factory),
            new StringLocalizer<AuthMessages>(factory),
            new StringLocalizer<AuthorizationMessages>(factory),
            new StringLocalizer<HrMessages>(factory),
            new StringLocalizer<WorkforceMessages>(factory),
            new StringLocalizer<RoomOperationsMessages>(factory),
            new StringLocalizer<TechnicalServiceMessages>(factory));
        var value = localizer[key];
        Assert.False(value.ResourceNotFound);
        Assert.Equal(expected, value.Value);
    }

    [Fact]
    public void PermissionCodes_AreNotResourceKeys()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("tr");
        var factory = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions()),
            NullLoggerFactory.Instance);
        var localizer = new StringLocalizer<HrMessages>(factory);
        Assert.True(localizer["hr.employee.manage"].ResourceNotFound);
    }

    [Fact]
    public void PropertyContextRequired_IsLocalized()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        var factory = new ResourceManagerStringLocalizerFactory(
            Options.Create(new LocalizationOptions()),
            NullLoggerFactory.Instance);
        var common = new StringLocalizer<CommonMessages>(factory);
        Assert.False(common["error.property-context-required.title"].ResourceNotFound);
    }
}
