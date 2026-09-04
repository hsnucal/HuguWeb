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
    [InlineData("tr", "error.leave-request-overlap.detail", "Başka bir bekleyen/onaylı talep veya kayıtlı izin bu tarihlerle çakışıyor.")]
        [InlineData("en", "error.leave-request-overlap.detail", "Another pending or approved leave request, or a recorded leave, covers one or more of these dates.")]
        [InlineData("ru", "error.leave-request-overlap.detail", "Другая ожидающая/одобренная заявка или записанный отпуск пересекается с этими датами.")]
        [InlineData("tr", "error.overlapping-primary-assignment.detail", "Seçtiğiniz geçerlilik tarihi mevcut çalışma geçmişiyle çakışıyor. Lütfen farklı bir tarih seçin.")]
        [InlineData("en", "error.overlapping-primary-assignment.detail", "The selected effective date conflicts with this employee’s work history. Please choose a different date.")]
        [InlineData("ru", "error.overlapping-primary-assignment.detail", "Выбранная дата вступления в силу пересекается с текущей трудовой историей. Выберите другую дату.")]
        [InlineData("tr", "error.invalid-transfer-date.detail", "Seçtiğiniz geçerlilik tarihi mevcut çalışma geçmişiyle çakışıyor. Lütfen farklı bir tarih seçin.")]
        [InlineData("en", "error.invalid-transfer-date.detail", "The selected effective date conflicts with this employee’s work history. Please choose a different date.")]
        [InlineData("ru", "error.invalid-transfer-date.detail", "Выбранная дата вступления в силу пересекается с текущей трудовой историей. Выберите другую дату.")]
        [InlineData("tr", "error.movement-target-not-promotion.detail", "Terfi için hedef pozisyon mevcut pozisyondan daha yüksek bir organizasyon seviyesinde olmalıdır.")]
        [InlineData("en", "error.movement-target-not-promotion.detail", "The promotion target must be at a higher organizational level than the current position.")]
        [InlineData("ru", "error.movement-target-not-promotion.detail", "Целевая должность при повышении должна быть на более высоком организационном уровне, чем текущая.")]
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
