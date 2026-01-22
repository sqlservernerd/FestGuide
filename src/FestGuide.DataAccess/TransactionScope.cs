using System.Data;
using FestGuide.DataAccess.Abstractions;

namespace FestGuide.DataAccess;

/// <summary>
/// Implementation of database transaction scope.
/// </summary>
internal sealed class TransactionScope : ITransactionScope
{
    private readonly IDbTransaction _transaction;
    private bool _disposed;
    private bool _completed;

    public TransactionScope(IDbTransaction transaction)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    /// <inheritdoc />
    public IDbTransaction Transaction => _transaction;

    /// <inheritdoc />
    public void Commit()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TransactionScope));
        }

        if (_completed)
        {
            throw new InvalidOperationException("Transaction has already been completed.");
        }

        _transaction.Commit();
        _completed = true;
    }

    /// <inheritdoc />
    public void Rollback()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TransactionScope));
        }

        if (_completed)
        {
            throw new InvalidOperationException("Transaction has already been completed.");
        }

        _transaction.Rollback();
        _completed = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // If not explicitly committed or rolled back, roll back on dispose
        if (!_completed)
        {
            try
            {
                _transaction.Rollback();
            }
            catch
            {
                // Suppress exceptions during rollback in disposal
                // The transaction may already be closed or in an invalid state
            }
        }

        // Use Dispose to clean up the transaction resource
        _transaction.Dispose();
        _disposed = true;
    }
}
