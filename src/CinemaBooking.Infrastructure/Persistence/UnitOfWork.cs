using CinemaBooking.Application.Common.Interfaces;

namespace CinemaBooking.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly CinemaBookingDbContext _dbContext;

    public UnitOfWork(CinemaBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
