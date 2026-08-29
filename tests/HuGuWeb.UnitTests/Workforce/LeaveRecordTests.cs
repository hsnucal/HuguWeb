using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class LeaveRecordTests
{
    private static readonly DateOnly Start = new(2026, 6, 1);
    private static readonly DateOnly End = new(2026, 6, 3);
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 8, 0, 0, TimeSpan.Zero);

    private static bool TryCreate(DateOnly start, DateOnly end, decimal amount, out LeaveRecord? record, out string? errorCode)
    {
        return LeaveRecord.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            start,
            end,
            amount,
            note: null,
            "actor",
            Now,
            out record,
            out _,
            out errorCode);
    }

    [Fact]
    public void TryCreate_Valid_Succeeds()
    {
        Assert.True(TryCreate(Start, End, 3.0m, out var record, out _));
        Assert.Equal(LeaveRecordStatus.Recorded, record!.Status);
    }

    [Fact]
    public void TryCreate_HalfDay_Succeeds()
    {
        Assert.True(TryCreate(Start, Start, 0.5m, out var record, out _));
        Assert.Equal(0.5m, record!.Amount);
    }

    [Fact]
    public void TryCreate_EndBeforeStart_IsRejected()
    {
        Assert.False(TryCreate(End, Start, 1.0m, out _, out var errorCode));
        Assert.Equal(LeaveValidation.Codes.LeaveInvalidDateRange, errorCode);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(0.25)]
    public void TryCreate_InvalidAmount_IsRejected(double amount)
    {
        Assert.False(TryCreate(Start, End, (decimal)amount, out _, out var errorCode));
        Assert.Equal(LeaveValidation.Codes.LeaveInvalidAmount, errorCode);
    }

    [Fact]
    public void TryCancel_Recorded_SetsCancellationMetadata()
    {
        Assert.True(TryCreate(Start, End, 3.0m, out var record, out _));

        var cancelled = record!.TryCancel("family reasons", "manager", Now.AddDays(1), out _, out _);

        Assert.True(cancelled);
        Assert.Equal(LeaveRecordStatus.Cancelled, record.Status);
        Assert.Equal("family reasons", record.CancellationReason);
        Assert.Equal("manager", record.CancelledByUserId);
        Assert.Equal(Now.AddDays(1), record.CancelledAtUtc);
    }

    [Fact]
    public void TryCancel_BlankReason_IsRejected()
    {
        Assert.True(TryCreate(Start, End, 3.0m, out var record, out _));

        var cancelled = record!.TryCancel("   ", "manager", Now, out var field, out var errorCode);

        Assert.False(cancelled);
        Assert.Equal(LeaveValidation.Fields.CancellationReason, field);
        Assert.Equal(LeaveValidation.Codes.LeaveCancellationReasonRequired, errorCode);
        Assert.Equal(LeaveRecordStatus.Recorded, record.Status);
    }

    [Fact]
    public void TryCancel_AlreadyCancelled_IsRejected()
    {
        Assert.True(TryCreate(Start, End, 3.0m, out var record, out _));
        Assert.True(record!.TryCancel("first", "manager", Now, out _, out _));

        var again = record.TryCancel("second", "manager", Now.AddDays(1), out _, out var errorCode);

        Assert.False(again);
        Assert.Equal(LeaveValidation.Codes.LeaveAlreadyCancelled, errorCode);
    }

    [Fact]
    public void Overlap_InclusiveRanges_AreDetected()
    {
        Assert.True(TryCreate(Start, End, 3.0m, out var recorded, out _));
        var existing = new[] { recorded! };

        Assert.True(LeaveOverlap.OverlapsAnyRecorded(existing, End, End.AddDays(2)));
        Assert.True(LeaveOverlap.OverlapsAnyRecorded(existing, Start, Start));
        Assert.False(LeaveOverlap.OverlapsAnyRecorded(existing, End.AddDays(1), End.AddDays(2)));
    }

    [Fact]
    public void Overlap_IgnoresCancelledRecords()
    {
        Assert.True(TryCreate(Start, End, 3.0m, out var recorded, out _));
        Assert.True(recorded!.TryCancel("cancelled", "manager", Now, out _, out _));

        Assert.False(LeaveOverlap.OverlapsAnyRecorded(new[] { recorded }, Start, End));
    }
}
