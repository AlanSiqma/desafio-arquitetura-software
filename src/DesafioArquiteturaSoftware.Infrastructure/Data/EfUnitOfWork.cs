using DesafioArquiteturaSoftware.Domain.Exceptions;
using DesafioArquiteturaSoftware.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace DesafioArquiteturaSoftware.Infrastructure.Data;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public EfUnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException(
                "A transaction is already active.");
        }

        _transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException();
        }
        catch (DbUpdateException exception)
            when (IsIdempotencyViolation(exception))
        {
            throw new IdempotencyKeyAlreadyExistsException();
        }
    }

    public async Task CommitAsync(
        CancellationToken cancellationToken)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException(
                "Transaction was not started.");
        }

        try
        {
            await _transaction.CommitAsync(
                cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(
        CancellationToken cancellationToken)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(
                cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void ClearTracking()
    {
        _context.ChangeTracker.Clear();
    }

    private static bool IsIdempotencyViolation(
        DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
        {
            return false;
        }

        return postgresException.SqlState ==
                   PostgresErrorCodes.UniqueViolation
               && postgresException.ConstraintName ==
                   "ux_idempotency_records_key";
    }
}