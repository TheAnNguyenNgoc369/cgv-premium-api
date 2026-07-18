using CinemaBooking.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

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
        // Reuse the ambient transaction when the caller is already inside one (e.g. PaymentService
        // wraps FinalizeSuccessfulBookingAsync, which now delegates loyalty into a nested
        // ExecuteInTransactionAsync). Opening a second EF transaction on the same DbContext throws.
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return await operation();
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var result = await operation();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
