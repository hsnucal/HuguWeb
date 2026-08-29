using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class PaymentIbanTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("TR")]
    public void TryNormalize_RejectsNullOrEmptyOrPrefixOnly(string? input)
    {
        // Existing DTO semantics: a payment profile row always stores a concrete IBAN.
        // Optional payment means the profile is not saved, not that IBAN may be null on save.
        Assert.False(PaymentIban.TryNormalize(input, out var normalized, out var error));
        Assert.Equal(string.Empty, normalized);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void TryNormalize_AcceptsTrPlus24Digits()
    {
        Assert.True(PaymentIban.TryNormalize(
            "TR330006100519786457841326",
            out var normalized,
            out var error));
        Assert.Equal("TR330006100519786457841326", normalized);
        Assert.Null(error);
    }

    [Fact]
    public void TryNormalize_AcceptsSpacedFormattedInput()
    {
        Assert.True(PaymentIban.TryNormalize(
            "TR33 0006 1005 1978 6457 8413 26",
            out var normalized,
            out _));
        Assert.Equal("TR330006100519786457841326", normalized);
    }

    [Fact]
    public void TryNormalize_AcceptsDigitsOnlyWithoutPrefix()
    {
        Assert.True(PaymentIban.TryNormalize(
            "330006100519786457841326",
            out var normalized,
            out _));
        Assert.Equal("TR330006100519786457841326", normalized);
    }

    [Fact]
    public void TryNormalize_AcceptsLowercaseAndPunctuationNoise()
    {
        Assert.True(PaymentIban.TryNormalize(
            "tr33-0006-1005-1978-6457-8413-26",
            out var normalized,
            out _));
        Assert.Equal("TR330006100519786457841326", normalized);
    }

    [Fact]
    public void TryNormalize_RejectsTrPlus23Digits()
    {
        Assert.False(PaymentIban.TryNormalize(
            "TR33000610051978645784132",
            out _,
            out _));
    }

    [Fact]
    public void TryNormalize_RejectsTrPlus25Digits()
    {
        Assert.False(PaymentIban.TryNormalize(
            "TR3300061005197864578413269",
            out _,
            out _));
    }

    [Fact]
    public void TryNormalize_RejectsLettersInBodyAsIncompleteAfterStrip()
    {
        // Letters are stripped; remaining digit count must still be exactly 24.
        Assert.False(PaymentIban.TryNormalize("TR33ABCD", out _, out _));
        Assert.False(PaymentIban.TryNormalize("BAD", out _, out _));
    }

    [Fact]
    public void TryNormalize_RejectsNonTurkishCountryPrefixPayload()
    {
        Assert.False(PaymentIban.TryNormalize(
            "DE89370400440532013000",
            out _,
            out _));
    }

    [Fact]
    public void TryNormalize_AcceptsPoSpacedDisplayValue()
    {
        // Exact PO acceptance case: presentation spaces must not affect structural validity.
        Assert.True(PaymentIban.TryNormalize(
            "TR 12 3123 1231 2312 3213 2131 32",
            out var normalized,
            out var error));
        Assert.Equal("TR123123123123123213213132", normalized);
        Assert.Equal(26, normalized.Length);
        Assert.Null(error);
    }

    [Fact]
    public async Task PaymentProfile_SavesPoSpacedCanonicalIban()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(harness.HireWithProfileCommand(), CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var useCase = new SaveEmployeePaymentProfileUseCase(harness.Store, harness.Workplace);
        var saved = await useCase.ExecuteAsync(
            new SaveEmployeePaymentProfileCommand(
                hired.Value.EmployeeId,
                "TR 12 3123 1231 2312 3213 2131 32",
                null,
                CanWriteSensitive: true),
            CancellationToken.None);

        Assert.True(saved.IsSuccess);
        Assert.Equal("TR123123123123123213213132", saved.Value.Iban);
    }

    [Fact]
    public void NormalizeTurkishIbanDigits_DoesNotDuplicateTr()
    {
        Assert.Equal(
            "330006100519786457841326",
            PaymentIban.NormalizeTurkishIbanDigits("TRTR330006100519786457841326"));
        Assert.Equal(
            "TR330006100519786457841326",
            PaymentIban.ToCanonical("tr33 0006 1005 1978 6457 8413 26"));
        Assert.Equal(string.Empty, PaymentIban.ToCanonical("TR"));
        Assert.Equal(
            "TR123123123123123213213132",
            PaymentIban.ToCanonical("TR 12 3123 1231 2312 3213 2131 32"));
    }
}