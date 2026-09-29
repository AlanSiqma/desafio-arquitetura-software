namespace DesafioArquiteturaSoftware.Domain.Interfaces;

public interface IUnitOfWork
{
    Task BeginTransactionAsync(
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);

    Task CommitAsync(
        CancellationToken cancellationToken);

    Task RollbackAsync(
        CancellationToken cancellationToken);

    void ClearTracking();
}