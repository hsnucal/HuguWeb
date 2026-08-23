using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

public interface ITechnicalServiceWorkplace
{
    Guid PropertyId { get; }
    bool IsConfigured { get; }
}

public sealed record AssignableEmployee(
    Guid EmployeeId,
    string GivenName,
    string FamilyName,
    string PersonnelNumber);

public interface IAssignableEmployeeDirectory
{
    Task<AssignableEmployee?> FindAssignableAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AssignableEmployee>> ListAssignableAsync(CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, AssignableEmployee>> GetEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken);
}

public sealed record KnownRoom(Guid RoomId, Guid PropertyId, string Number, bool IsActive);

public interface IRoomIdentityDirectory
{
    Task<KnownRoom?> FindAsync(Guid roomId, CancellationToken cancellationToken);
    Task<IReadOnlyList<KnownRoom>> ListActiveAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, KnownRoom>> GetAsync(
        IReadOnlyCollection<Guid> roomIds,
        CancellationToken cancellationToken);
}

public interface IRoomPreparationImpactConsumer
{
    Task<TechnicalServiceResult<bool>> EnsurePreparationRequiredAsync(
        Guid roomId,
        Guid actorUserId,
        CancellationToken cancellationToken);
}

public interface ITechnicalServiceStore
{
    Task<MaintenanceIssue?> GetIssueAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaintenanceIssue>> ListIssuesAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaintenanceIssue>> ListIssuesForRoomAsync(Guid roomId, CancellationToken cancellationToken);
    Task<MaintenanceIssueCategory?> GetCategoryAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaintenanceIssueCategory>> ListCategoriesAsync(
        Guid propertyId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<MaintenanceIssueHistoryEntry>> ListHistoryAsync(
        Guid issueId,
        CancellationToken cancellationToken);

    void AddIssue(MaintenanceIssue issue);
    void AddCategory(MaintenanceIssueCategory category);
    void AddHistory(MaintenanceIssueHistoryEntry entry);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class IssueConcurrencyConflictException : Exception
{
    public IssueConcurrencyConflictException()
        : base("The technical issue was changed by a concurrent operation.")
    {
    }
}
