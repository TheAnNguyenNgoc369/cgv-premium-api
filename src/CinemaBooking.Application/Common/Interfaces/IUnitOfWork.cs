namespace CinemaBooking.Application.Common.Interfaces;

public interface IUnitOfWork
{
    Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes tracked changes to the database. Required for entity mutations that are not
    /// persisted by a repository method (e.g. incrementing a locked entity inside a transaction),
    /// because committing a transaction does not itself flush pending changes.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
