using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class LeaveEntitlementTests
{
    private static readonly DateOnly EffectiveDate = new(2026, 1, 1);
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(LeaveEntitlementSource.Entitlement, 14.0)]
    [InlineData(LeaveEntitlementSource.CarryOver, 2.0)]
    [InlineData(LeaveEntitlementSource.ManualAdjustment, 0.5)]
    public void TryCreate_ValidPositiveAmounts_Succeeds(LeaveEntitlementSource source, double amount)
    {
        var note = source == LeaveEntitlementSource.ManualAdjustment ? "correction" : null;
        var created = LeaveEntitlement.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            EffectiveDate,
            (decimal)amount,
            source,
            note,
            "actor",
            Now,
            out var entitlement,
            out _,
            out _);

        Assert.True(created);
        Assert.Equal((decimal)amount, entitlement!.Amount);
    }

    [Fact]
    public void TryCreate_ManualAdjustmentNegative_Succeeds()
    {
        var created = LeaveEntitlement.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            EffectiveDate,
            -1.0m,
            LeaveEntitlementSource.ManualAdjustment,
            "clawback",
            "actor",
            Now,
            out var entitlement,
            out _,
            out _);

        Assert.True(created);
        Assert.Equal(-1.0m, entitlement!.Amount);
    }

    [Theory]
    [InlineData(LeaveEntitlementSource.Entitlement, -1.0)]
    [InlineData(LeaveEntitlementSource.CarryOver, -2.0)]
    public void TryCreate_NonAdjustmentNegative_IsRejected(LeaveEntitlementSource source, double amount)
    {
        var created = LeaveEntitlement.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            EffectiveDate,
            (decimal)amount,
            source,
            null,
            "actor",
            Now,
            out _,
            out var field,
            out var errorCode);

        Assert.False(created);
        Assert.Equal(LeaveValidation.Fields.Amount, field);
        Assert.Equal(LeaveValidation.Codes.LeaveEntitlementInvalidAmount, errorCode);
    }

    [Fact]
    public void TryCreate_ZeroAmount_IsRejected()
    {
        var created = LeaveEntitlement.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            EffectiveDate,
            0m,
            LeaveEntitlementSource.ManualAdjustment,
            "note",
            "actor",
            Now,
            out _,
            out _,
            out var errorCode);

        Assert.False(created);
        Assert.Equal(LeaveValidation.Codes.LeaveEntitlementInvalidAmount, errorCode);
    }

    [Theory]
    [InlineData(0.2)]
    [InlineData(1.25)]
    [InlineData(0.333)]
    public void TryCreate_NonHalfDayPrecision_IsRejected(double amount)
    {
        var created = LeaveEntitlement.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            EffectiveDate,
            (decimal)amount,
            LeaveEntitlementSource.Entitlement,
            null,
            "actor",
            Now,
            out _,
            out _,
            out var errorCode);

        Assert.False(created);
        Assert.Equal(LeaveValidation.Codes.LeaveEntitlementInvalidAmount, errorCode);
    }

    [Fact]
    public void TryCreate_ManualAdjustmentWithoutNote_IsRejected()
    {
        var created = LeaveEntitlement.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            EffectiveDate,
            1.0m,
            LeaveEntitlementSource.ManualAdjustment,
            "   ",
            "actor",
            Now,
            out _,
            out var field,
            out var errorCode);

        Assert.False(created);
        Assert.Equal(LeaveValidation.Fields.Note, field);
        Assert.Equal(LeaveValidation.Codes.LeaveEntitlementNoteRequired, errorCode);
    }
}
