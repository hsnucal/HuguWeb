using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class LeaveTypeTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TryCreateCustom_NormalizesCode_AndLeavesSystemKindNull()
    {
        var created = LeaveType.TryCreateCustom(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "  Custom-Study  ",
            "  Study Leave  ",
            tracksBalance: false,
            "actor",
            Now,
            out var leaveType,
            out _,
            out _);

        Assert.True(created);
        Assert.Equal("custom-study", leaveType!.Code);
        Assert.Equal("Study Leave", leaveType.Name);
        Assert.Null(leaveType.SystemKind);
        Assert.True(leaveType.IsActive);
    }

    [Fact]
    public void TryCreateCustom_BlankCode_IsRejected()
    {
        var created = LeaveType.TryCreateCustom(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "   ",
            "Name",
            tracksBalance: false,
            "actor",
            Now,
            out _,
            out var field,
            out var errorCode);

        Assert.False(created);
        Assert.Equal(LeaveValidation.Fields.Code, field);
        Assert.Equal(LeaveValidation.Codes.LeaveTypeCodeRequired, errorCode);
    }

    [Fact]
    public void CreateSystemDefault_KeepsSystemKind()
    {
        var leaveType = LeaveType.CreateSystemDefault(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "annual",
            "Yıllık İzin",
            LeaveTypeSystemKind.Annual,
            tracksBalance: true,
            "system",
            Now);

        Assert.Equal(LeaveTypeSystemKind.Annual, leaveType.SystemKind);
        Assert.True(leaveType.TracksBalance);
        Assert.Equal("annual", leaveType.Code);
    }

    [Fact]
    public void NormalizeCodeForLookup_IsCaseInsensitive()
    {
        Assert.Equal(
            LeaveType.NormalizeCodeForLookup("ANNUAL"),
            LeaveType.NormalizeCodeForLookup("annual"));
    }

    [Fact]
    public void TryRename_UpdatesNameOnly_CodeImmutable()
    {
        var leaveType = LeaveType.CreateSystemDefault(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "annual",
            "Yıllık İzin",
            LeaveTypeSystemKind.Annual,
            tracksBalance: true,
            "system",
            Now);

        var renamed = leaveType.TryRename("Annual Leave", "actor", Now.AddDays(1), out _, out _);

        Assert.True(renamed);
        Assert.Equal("Annual Leave", leaveType.Name);
        Assert.Equal("annual", leaveType.Code);
    }

    [Fact]
    public void TrySetTracksBalance_WithHistoricalUsage_IsRejected()
    {
        var leaveType = LeaveType.CreateSystemDefault(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "annual",
            "Yıllık İzin",
            LeaveTypeSystemKind.Annual,
            tracksBalance: true,
            "system",
            Now);

        var changed = leaveType.TrySetTracksBalance(
            tracksBalance: false,
            hasHistoricalUsage: true,
            "actor",
            Now,
            out var field,
            out var errorCode);

        Assert.False(changed);
        Assert.Equal(LeaveValidation.Fields.TracksBalance, field);
        Assert.Equal(LeaveValidation.Codes.LeaveTypeHasHistory, errorCode);
        Assert.True(leaveType.TracksBalance);
    }

    [Fact]
    public void TrySetTracksBalance_WithoutHistory_IsAllowed()
    {
        var created = LeaveType.TryCreateCustom(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "study",
            "Study",
            tracksBalance: false,
            "actor",
            Now,
            out var leaveType,
            out _,
            out _);
        Assert.True(created);

        var changed = leaveType!.TrySetTracksBalance(
            tracksBalance: true,
            hasHistoricalUsage: false,
            "actor",
            Now,
            out _,
            out _);

        Assert.True(changed);
        Assert.True(leaveType.TracksBalance);
    }

    [Fact]
    public void Deactivate_MarksInactive()
    {
        var created = LeaveType.TryCreateCustom(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "study",
            "Study",
            tracksBalance: false,
            "actor",
            Now,
            out var leaveType,
            out _,
            out _);
        Assert.True(created);

        leaveType!.Deactivate("actor", Now.AddDays(1));

        Assert.False(leaveType.IsActive);
    }
}
