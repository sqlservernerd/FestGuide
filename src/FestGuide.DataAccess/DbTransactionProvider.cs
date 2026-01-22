using System.Data;
using FestGuide.DataAccess.Abstractions;

namespace FestGuide.DataAccess;

/// <summary>
/// Provides database transaction management using the current database connection.
/// </summary>
public class DbTransactionProvider : IDbTransactionProvider
{
    private readonly IDbConnection _connection;

    public DbTransactionProvider(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public ITransactionScope BeginTransaction()
    {
        // Ensure connection is open
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }

        var transaction = _connection.BeginTransaction();
        return new TransactionScope(transaction);
    }
}
