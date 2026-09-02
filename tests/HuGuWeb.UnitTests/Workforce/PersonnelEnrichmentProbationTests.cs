using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class PersonnelEnrichmentProbationTests
{
    [Fact]
    public void Probation_TwoMonthsWithStart_ComputesEndDate()
    {
        var employment = Employment.Open(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 1, 15));

        Assert.True(employment.TryApplyWorkforceTerms(
            Terms(probationMonths: 2, probationStart: new DateOnly(2026, 1, 15)),
            out _,
            out _));
        Assert.Equal(2, employment.ProbationPeriodMonths);
        Assert.Equal(new DateOnly(2026, 1, 15), employment.ProbationStartDate);
        Assert.Equal(new DateOnly(2026, 3, 15), employment.ProbationEndDate);
    }

    [Fact]
    public void Probation_NullMonths_ClearsStartAndEnd()
    {
        var employment = Employment.Open(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 1, 15));
        Assert.True(employment.TryApplyWorkforceTerms(
            Terms(probationMonths: 2, probationStart: new DateOnly(2026, 1, 15)),
            out _,
            out _));

        Assert.True(employment.TryApplyWorkforceTerms(Terms(), out _, out _));
        Assert.Null(employment.ProbationPeriodMonths);
        Assert.Null(employment.ProbationStartDate);
        Assert.Null(employment.ProbationEndDate);
    }

    [Fact]
    public void Probation_MonthsNotTwo_IsRejected()
    {
        var employment = Employment.Open(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 1, 15));

        Assert.False(employment.TryApplyWorkforceTerms(
            Terms(probationMonths: 3, probationStart: new DateOnly(2026, 1, 15)),
            out var field,
            out var code));
        Assert.Equal(HrValidation.Fields.ProbationPeriodMonths, field);
        Assert.Equal(HrValidation.Codes.ProbationPeriodMonthsInvalid, code);
    }

    [Fact]
    public void Probation_TwoMonthsWithoutStart_IsRejected()
    {
        var employment = Employment.Open(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 1, 15));

        Assert.False(employment.TryApplyWorkforceTerms(
            Terms(probationMonths: 2),
            out var field,
            out var code));
        Assert.Equal(HrValidation.Fields.ProbationStartDate, field);
        Assert.Equal(HrValidation.Codes.ProbationStartDateRequired, code);
    }

    [Fact]
    public void Probation_StartWithoutMonths_IsRejected()
    {
        var employment = Employment.Open(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 1, 15));

        Assert.False(employment.TryApplyWorkforceTerms(
            Terms(probationStart: new DateOnly(2026, 1, 15)),
            out var field,
            out var code));
        Assert.Equal(HrValidation.Fields.ProbationStartDate, field);
        Assert.Equal(HrValidation.Codes.ProbationStartDateMustBeNull, code);
    }

    [Fact]
    public async Task Update_PersistsProbationOnCard()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var updated = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId,
                "Ayşe",
                "Yılmaz",
                EmptyProfile(),
                CanWriteSensitive: true,
                WorkforceTerms: new EmploymentWorkforceWriteModel(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    WorkType.FullTime,
                    2,
                    new DateOnly(2026, 8, 21))),
            CancellationToken.None);
        Assert.True(updated.IsSuccess, updated.Error?.Detail);

        var card = await harness.HrCard.ExecuteAsync(hired.Value.EmployeeId, true, CancellationToken.None);
        Assert.Equal(2, card.Value!.WorkforceTerms!.ProbationPeriodMonths);
        Assert.Equal(new DateOnly(2026, 8, 21), card.Value.WorkforceTerms.ProbationStartDate);
        Assert.Equal(new DateOnly(2026, 10, 21), card.Value.WorkforceTerms.ProbationEndDate);
    }

    private static EmploymentWorkforceTermsValues Terms(
        int? probationMonths = null,
        DateOnly? probationStart = null) =>
        new(null, null, null, null, null, null, null, null, null, null, probationMonths, probationStart, null);

    private static HrProfileWriteModel EmptyProfile() =>
        new(
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, []);
}
