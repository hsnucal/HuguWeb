using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class PersonnelEnrichmentCertificateTests
{
    [Fact]
    public void TryCreate_RequiresName()
    {
        Assert.False(EmployeeCertificate.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "  ",
            0,
            DateTimeOffset.UtcNow,
            out _,
            out _,
            out var error));
        Assert.Equal(HrValidation.Codes.CertificateNameRequired, error);
    }

    [Fact]
    public async Task Hire_PersistsCertificates_ReplaceAllOnUpdate()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(
                certificates:
                [
                    new EmployeeCertificateDraft(Guid.Empty, "İlk Yardım"),
                    new EmployeeCertificateDraft(Guid.Empty, "Hijyen")
                ]),
            CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        Assert.Equal(2, harness.Store.EmployeeCertificates.Count);

        var updated = await harness.UpdateProfile.ExecuteAsync(
            new UpdateEmployeeHrProfileCommand(
                hired.Value!.EmployeeId,
                "Ayşe",
                "Yılmaz",
                EmptyProfile(),
                CanWriteSensitive: true,
                Certificates: [new EmployeeCertificateDraft(Guid.Empty, "Yangın")]),
            CancellationToken.None);
        Assert.True(updated.IsSuccess, updated.Error?.Detail);
        Assert.Single(harness.Store.EmployeeCertificates);
        Assert.Equal("Yangın", harness.Store.EmployeeCertificates[0].Name);

        var card = await harness.HrCard.ExecuteAsync(hired.Value.EmployeeId, true, CancellationToken.None);
        Assert.Single(card.Value!.Certificates);
        Assert.Equal("Yangın", card.Value.Certificates[0].Name);
    }

    private static HrProfileWriteModel EmptyProfile() =>
        new(
            null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, []);
}
