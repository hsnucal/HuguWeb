namespace HuGuWeb.Workforce.Application;

public interface IWorkforceTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
