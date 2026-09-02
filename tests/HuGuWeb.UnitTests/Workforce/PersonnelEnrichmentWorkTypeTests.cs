using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class PersonnelEnrichmentWorkTypeTests
{
    [Fact]
    public void Open_DefaultsWorkTypeToFullTime()
    {
        var employment = Employment.Open(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 8, 21),
            new DateOnly(2026, 8, 21));

        Assert.Equal(WorkType.FullTime, employment.WorkType);
    }

    [Theory]
    [InlineData(WorkType.FullTime)]
    [InlineData(WorkType.PartTime)]
    [InlineData(WorkType.ReducedHours)]
    [InlineData(WorkType.Intern)]
    public void Open_AcceptsExplicitWorkType(WorkType workType)
    {
        var employment = Employment.Open(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 8, 21),
            new DateOnly(2026, 8, 21),
            workType);

        Assert.Equal(workType, employment.WorkType);
    }

    [Fact]
    public void WorkType_IsDistinctFromContractType()
    {
        var employment = Employment.Open(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 8, 21),
            new DateOnly(2026, 8, 21),
            WorkType.PartTime);

        Assert.True(employment.TryApplyWorkforceTerms(
            new EmploymentWorkforceTermsValues(
                EmploymentContractType.Indefinite,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                WorkType.PartTime),
            out _,
            out _));
        Assert.Equal(WorkType.PartTime, employment.WorkType);
        Assert.Equal(EmploymentContractType.Indefinite, employment.ContractType);
    }

    [Fact]
    public async Task Hire_DefaultsWorkTypeToFullTime()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        Assert.Equal(WorkType.FullTime, harness.Store.Employments[0].WorkType);
    }

    [Fact]
    public async Task HireWithProfile_AppliesRequestedWorkType()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(
                workforceTerms: new EmploymentWorkforceWriteModel(
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    WorkType.Intern)),
            CancellationToken.None);

        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        Assert.Equal(WorkType.Intern, harness.Store.Employments[0].WorkType);
        var card = await harness.HrCard.ExecuteAsync(hired.Value!.EmployeeId, true, CancellationToken.None);
        Assert.True(card.IsSuccess);
        Assert.Equal(WorkType.Intern, card.Value!.WorkforceTerms!.WorkType);
    }

    [Fact]
    public void Apply_NullWorkType_KeepsExisting()
    {
        var employment = Employment.Open(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            new DateOnly(2026, 8, 21),
            new DateOnly(2026, 8, 21),
            WorkType.ReducedHours);

        Assert.True(EmploymentWorkforceComposer.Apply(employment, EmploymentWorkforceWriteModel.Empty).IsSuccess);
        Assert.Equal(WorkType.ReducedHours, employment.WorkType);
    }
}
