using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class PersonnelNumberAllocationTests
{
    [Fact]
    public async Task NewHire_GetsGeneratedNumericPersonnelNumber()
    {
        var harness = new WorkforceHarness();

        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        Assert.Equal("1001", hired.Value!.PersonnelNumber);
        Assert.NotEqual(hired.Value.EmployeeId.ToString(), hired.Value.PersonnelNumber);
    }

    [Fact]
    public async Task SubsequentHire_GetsNextNumber()
    {
        var harness = new WorkforceHarness();
        await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        var second = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.Equal("1002", second.Value!.PersonnelNumber);
    }

    [Fact]
    public async Task GeneratedNumbers_AreUniqueWithinOrganization()
    {
        var harness = new WorkforceHarness();
        var first = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        var second = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.NotEqual(first.Value!.PersonnelNumber, second.Value!.PersonnelNumber);
        Assert.Equal(2, harness.Store.Employees.Select(item => item.PersonnelNumber).Distinct().Count());
    }

    [Fact]
    public async Task Sequences_AreIndependentPerOrganization()
    {
        var first = new WorkforceHarness();
        var second = new WorkforceHarness();

        var hiredFirst = await first.Hire.ExecuteAsync(first.HireCommand(), CancellationToken.None);
        var hiredSecond = await second.Hire.ExecuteAsync(second.HireCommand(), CancellationToken.None);

        Assert.True(hiredFirst.IsSuccess);
        Assert.True(hiredSecond.IsSuccess);
        Assert.Equal("1001", hiredFirst.Value!.PersonnelNumber);
        Assert.Equal("1001", hiredSecond.Value!.PersonnelNumber);
        Assert.NotEqual(first.OrganizationId, second.OrganizationId);
    }

    [Fact]
    public async Task ConcurrentAllocations_DoNotReuseNumbers()
    {
        var harness = new WorkforceHarness();
        var tasks = Enumerable.Range(0, 8)
            .Select(_ => harness.Store.AllocatePersonnelNumberAsync(harness.OrganizationId, CancellationToken.None))
            .ToArray();

        var numbers = await Task.WhenAll(tasks);

        Assert.Equal(8, numbers.Distinct().Count());
        Assert.Equal(["1001", "1002", "1003", "1004", "1005", "1006", "1007", "1008"], numbers.OrderBy(item => item).ToArray());
    }

    [Fact]
    public async Task ExistingAlphanumericPersonnelNumber_IsUnchanged()
    {
        var harness = new WorkforceHarness();
        var historic = harness.SeedEmployee("DEV-2001", "Can", "Yılmaz");

        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.True(hired.IsSuccess);
        Assert.Equal("DEV-2001", historic.PersonnelNumber);
        Assert.Equal("1001", hired.Value!.PersonnelNumber);
    }

    [Fact]
    public async Task OccupiedNumericHistoricValue_IsSkipped()
    {
        var harness = new WorkforceHarness();
        harness.SeedEmployee("1001");

        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.True(hired.IsSuccess);
        Assert.Equal("1002", hired.Value!.PersonnelNumber);
        Assert.Contains(harness.Store.Employees, item => item.PersonnelNumber == "1001");
    }

    [Fact]
    public async Task TerminatedPersonnelNumber_IsNeverReused()
    {
        var harness = new WorkforceHarness();
        var first = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        Assert.True((await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(first.Value!.EmployeeId, harness.Clock.Today, EmploymentTerminationReason.Resignation),
            CancellationToken.None)).IsSuccess);

        var second = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.Equal("1001", first.Value.PersonnelNumber);
        Assert.Equal("1002", second.Value!.PersonnelNumber);
        Assert.Contains(harness.Store.Employees, item => item.PersonnelNumber == "1001");
    }

    [Fact]
    public async Task ProfileHire_AlsoGeneratesPersonnelNumber()
    {
        var harness = new WorkforceHarness();

        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);

        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        Assert.Equal("1001", hired.Value!.PersonnelNumber);
    }
}
