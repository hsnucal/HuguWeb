using HuGuWeb.Api.Diagnostics;

namespace HuGuWeb.UnitTests;

public class CorrelationIdTests
{
    [Fact]
    public void Resolve_UsesIncomingValue_WhenSafe()
    {
        var resolved = CorrelationId.Resolve("req-abc.123_OK", "fallback");

        Assert.Equal("req-abc.123_OK", resolved);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("has space")]
    [InlineData("bad\nnewline")]
    [InlineData("header:injection")]
    public void Resolve_IgnoresUnsafeIncomingValues(string? incoming)
    {
        var resolved = CorrelationId.Resolve(incoming, "trace-fallback");

        Assert.Equal("trace-fallback", resolved);
    }

    [Fact]
    public void Resolve_IgnoresIncomingValuesLongerThanLimit()
    {
        var incoming = new string('a', CorrelationId.MaxLength + 1);

        var resolved = CorrelationId.Resolve(incoming, "fallback");

        Assert.Equal("fallback", resolved);
    }

    [Fact]
    public void IsSafe_AcceptsMaxLengthToken()
    {
        var incoming = new string('b', CorrelationId.MaxLength);

        Assert.True(CorrelationId.IsSafe(incoming));
    }
}
