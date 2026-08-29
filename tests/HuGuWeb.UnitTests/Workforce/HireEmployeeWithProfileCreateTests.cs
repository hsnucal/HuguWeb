using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class HireEmployeeWithProfileCreateTests
{
    [Fact]
    public async Task FirstEmployee_WhenStoreHasZeroEmployees_CanBeCreated()
    {
        var harness = new WorkforceHarness();
        Assert.Empty(harness.Store.Employees);

        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Single(harness.Store.Employees);
        Assert.Single(harness.Store.Employments);
        Assert.Single(harness.Store.Assignments);
        Assert.Equal(AssignmentKind.Primary, harness.Store.Assignments[0].Kind);
        Assert.Null(harness.Store.Assignments[0].EndDate);
        Assert.Equal(harness.Store.Employees[0].Id, harness.Store.Employments[0].EmployeeId);
        Assert.Equal(harness.Store.Employments[0].Id, harness.Store.Assignments[0].EmploymentId);
        Assert.Empty(harness.Store.PaymentProfiles);
    }

    [Fact]
    public async Task PersonnelNumber_AfterEmptySequence_StartsAtConfiguredBusinessNumber()
    {
        var harness = new WorkforceHarness();
        Assert.Empty(harness.Store.Sequences);
        Assert.Empty(harness.Store.Employees);

        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(PersonnelNumber.Format(PersonnelNumberSequence.StartingValue), result.Value!.PersonnelNumber);
        Assert.Equal(PersonnelNumberSequence.StartingValue + 1, harness.Store.Sequences[harness.OrganizationId].NextValue);
    }

    [Fact]
    public async Task SeniorityStartDate_Null_DoesNotBlockCreate()
    {
        var harness = new WorkforceHarness();

        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(seniorityStartDate: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Null(harness.Store.Employments[0].SeniorityStartDate);
        Assert.Equal(harness.Store.Employments[0].StartDate, harness.Store.Employments[0].EffectiveSeniorityDate);
    }

    [Fact]
    public async Task SeniorityStartDate_OnOrBeforeStart_AllowsCreate()
    {
        var harness = new WorkforceHarness();
        var start = harness.Clock.Today;

        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(startDate: start, seniorityStartDate: start.AddDays(-10)),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(start.AddDays(-10), harness.Store.Employments[0].SeniorityStartDate);
    }

    [Fact]
    public async Task SeniorityStartDate_AfterStart_IsRejected_AndLeavesNoPartialData()
    {
        var harness = new WorkforceHarness();
        var start = harness.Clock.Today;

        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(startDate: start, seniorityStartDate: start.AddDays(1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(HrValidation.Codes.SeniorityStartDateInvalid, result.Error!.Code);
        AssertNoPersonnelCreated(harness);
        Assert.Empty(harness.Store.Sequences);
    }

    [Fact]
    public async Task ValidDepartmentPositionApplicability_AllowsCreate()
    {
        var harness = new WorkforceHarness();

        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(
                departmentId: harness.OtherDepartmentId,
                positionId: harness.PositionId),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(harness.OtherDepartmentId, result.Value!.DepartmentId);
        Assert.Equal(harness.PositionId, result.Value.PositionId);
    }

    [Fact]
    public async Task InvalidDepartmentPositionCombination_RemainsRejected_AndLeavesNoPartialData()
    {
        var harness = new WorkforceHarness();

        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(
                departmentId: harness.DepartmentId,
                positionId: harness.OtherPositionId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("position-not-available-for-department", result.Error!.Code);
        AssertNoPersonnelCreated(harness);
    }

    [Fact]
    public async Task ContractFields_CanBeSuppliedOnCreate()
    {
        var harness = new WorkforceHarness();
        var start = harness.Clock.Today;

        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(
                startDate: start,
                workforceTerms: Terms(EmploymentContractType.FixedTerm, start.AddMonths(6)),
                seniorityStartDate: start.AddDays(-3)),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        var employment = harness.Store.Employments.Single();
        Assert.Equal(EmploymentContractType.FixedTerm, employment.ContractType);
        Assert.Equal(start.AddMonths(6), employment.ContractEndDate);
        Assert.Equal(start.AddDays(-3), employment.SeniorityStartDate);
        Assert.Null(employment.PartTimeMonthlyHours);
    }

    [Fact]
    public async Task InvalidContractOnCreate_LeavesNoPartialData_ThenValidCreateSucceeds()
    {
        var harness = new WorkforceHarness();
        var start = harness.Clock.Today;

        var rejected = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(
                startDate: start,
                workforceTerms: Terms(EmploymentContractType.FixedTerm, contractEnd: null)),
            CancellationToken.None);

        Assert.False(rejected.IsSuccess);
        Assert.Equal(HrValidation.Codes.ContractEndDateRequired, rejected.Error!.Code);
        AssertNoPersonnelCreated(harness);
        Assert.Empty(harness.Store.Sequences);

        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);

        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        Assert.Equal("1001", hired.Value!.PersonnelNumber);
        Assert.Single(harness.Store.Employees);
        Assert.Single(harness.Store.Employments);
        Assert.Single(harness.Store.Assignments);
        Assert.Empty(harness.Store.PaymentProfiles);
    }

    [Fact]
    public async Task InvalidOfficialOccupationOnCreate_IsRejected()
    {
        var harness = new WorkforceHarness();

        var result = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(officialProfile: harness.OfficialWrite(occupation: "not-a-code")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid-occupation-code", result.Error!.Code);
        Assert.Empty(harness.Store.OfficialEmploymentProfiles);
        Assert.Empty(harness.Store.EmploymentBesSettings);
    }

    [Fact]
    public async Task PaymentProfile_CanBeSavedAfterCreate_WithoutJoiningEmployee()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        Assert.Empty(harness.Store.PaymentProfiles);

        var saved = await new SaveEmployeePaymentProfileUseCase(harness.Store, harness.Workplace)
            .ExecuteAsync(
                new SaveEmployeePaymentProfileCommand(
                    hired.Value!.EmployeeId,
                    "TR33 0006 1005 1978 6457 8413 26",
                    "Test Bank",
                    CanWriteSensitive: true),
                CancellationToken.None);

        Assert.True(saved.IsSuccess, saved.Error?.Detail);
        Assert.Single(harness.Store.PaymentProfiles);
        Assert.Equal(hired.Value.EmployeeId, harness.Store.PaymentProfiles[0].EmployeeId);
        Assert.Equal(harness.OrganizationId, harness.Store.PaymentProfiles[0].OrganizationId);
        Assert.Equal("TR330006100519786457841326", harness.Store.PaymentProfiles[0].Iban);
        Assert.Equal("Test Bank", harness.Store.PaymentProfiles[0].BankName);

        var updated = await new SaveEmployeePaymentProfileUseCase(harness.Store, harness.Workplace)
            .ExecuteAsync(
                new SaveEmployeePaymentProfileCommand(
                    hired.Value.EmployeeId,
                    "TR330006100519786457841326",
                    "Updated Bank",
                    CanWriteSensitive: true),
                CancellationToken.None);
        Assert.True(updated.IsSuccess, updated.Error?.Detail);
        Assert.Single(harness.Store.PaymentProfiles);
        Assert.Equal("Updated Bank", harness.Store.PaymentProfiles[0].BankName);
    }

    [Fact]
    public async Task PaymentProfile_WithoutSensitivePermission_IsRejected()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);

        var saved = await new SaveEmployeePaymentProfileUseCase(harness.Store, harness.Workplace)
            .ExecuteAsync(
                new SaveEmployeePaymentProfileCommand(
                    hired.Value!.EmployeeId,
                    "TR330006100519786457841326",
                    null,
                    CanWriteSensitive: false),
                CancellationToken.None);

        Assert.False(saved.IsSuccess);
        Assert.Equal("sensitive-write-forbidden", saved.Error!.Code);
        Assert.Empty(harness.Store.PaymentProfiles);
    }

    [Fact]
    public void PersonnelCard_KeepsDepartmentAndPositionOnlyUnderWorkOrganization()
    {
        var source = File.ReadAllText(
            Path.Combine(FindRepoRoot(), "src", "frontend", "web", "src", "workforce", "PersonnelCard.tsx"));
        var general = Slice(source, "function GeneralTab(", "function IdentityTab(");
        var work = Slice(source, "function WorkTab(", "function PaymentTab(");
        var payment = Slice(source, "function PaymentTab(", "function HistoryTab(");

        Assert.DoesNotContain("id=\"hr-department\"", general);
        Assert.DoesNotContain("id=\"hr-position\"", general);
        Assert.DoesNotContain("id=\"hr-payment-iban\"", general);
        Assert.DoesNotContain("id=\"hr-work-department\"", general);
        Assert.DoesNotContain("id=\"hr-work-position\"", general);
        Assert.Contains("id=\"hr-work-department\"", work);
        Assert.Contains("id=\"hr-work-position\"", work);
        Assert.Contains("styles.factValue", work);
        Assert.Contains("t('workforce.transfer')", work);
        Assert.Contains("id=\"hr-payment-iban\"", payment);
        Assert.Contains("id=\"hr-payment-bank\"", payment);
        Assert.DoesNotContain("Employee.PropertyId", source);
        Assert.DoesNotContain("Employment.PropertyId", source);
    }

    private static void AssertNoPersonnelCreated(WorkforceHarness harness)
    {
        Assert.Empty(harness.Store.Employees);
        Assert.Empty(harness.Store.Employments);
        Assert.Empty(harness.Store.Assignments);
        Assert.Empty(harness.Store.HrProfiles);
        Assert.Empty(harness.Store.OfficialEmploymentProfiles);
        Assert.Empty(harness.Store.EmploymentBesSettings);
        Assert.Empty(harness.Store.EmergencyContacts);
    }

    private static EmploymentWorkforceWriteModel Terms(
        EmploymentContractType? contractType = null,
        DateOnly? contractEnd = null,
        decimal? partTimeHours = null) =>
        new(contractType, contractEnd, partTimeHours, null, null, null, null, null, null);

    private static string Slice(string source, string start, string end)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        var to = source.IndexOf(end, StringComparison.Ordinal);
        Assert.True(from >= 0 && to > from, $"Could not slice PersonnelCard between {start} and {end}.");
        return source[from..to];
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HuGuWeb.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
