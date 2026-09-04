using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public static class ReportingLineResolver
{
    public static async Task<WorkforceReportingLine?> ForEmploymentOnAsync(
        IWorkforceStore store,
        Guid employmentId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var lines = await store.ListReportingLinesForEmploymentAsync(employmentId, cancellationToken);
        return ReportingLines.Covering(lines, date);
    }
}
