using HuGuWeb.TechnicalService.Application;
using HuGuWeb.TechnicalService.Domain;
using Microsoft.EntityFrameworkCore;

namespace HuGuWeb.TechnicalService.Infrastructure.Persistence;

public sealed class EfTechnicalServiceStore(TechnicalServiceDbContext dbContext) : ITechnicalServiceStore
{
    public Task<MaintenanceIssue?> GetIssueAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Issues.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MaintenanceIssue>> ListIssuesAsync(
        Guid propertyId,
        CancellationToken cancellationToken) =>
        await dbContext.Issues
            .Where(item => item.PropertyId == propertyId)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<MaintenanceIssue>> ListIssuesForRoomAsync(
        Guid roomId,
        CancellationToken cancellationToken) =>
        await dbContext.Issues
            .Where(item => item.RoomId == roomId)
            .ToArrayAsync(cancellationToken);

    public Task<MaintenanceIssueCategory?> GetCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Categories.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IReadOnlyList<MaintenanceIssueCategory>> ListCategoriesAsync(
        Guid propertyId,
        CancellationToken cancellationToken) =>
        await dbContext.Categories
            .Where(item => item.PropertyId == propertyId)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<MaintenanceIssueHistoryEntry>> ListHistoryAsync(
        Guid issueId,
        CancellationToken cancellationToken) =>
        await dbContext.History
            .Where(item => item.IssueId == issueId)
            .ToArrayAsync(cancellationToken);

    public void AddIssue(MaintenanceIssue issue) => dbContext.Issues.Add(issue);

    public void AddCategory(MaintenanceIssueCategory category) => dbContext.Categories.Add(category);

    public void AddHistory(MaintenanceIssueHistoryEntry entry) => dbContext.History.Add(entry);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new IssueConcurrencyConflictException();
        }
    }
}
