using System.Data;
using FestConnect.DataAccess.Abstractions;

namespace FestConnect.DataAccess;

/// <summary>
/// Provides database transaction management using the current database connection.
/// </summary>
/// <remarks>
/// <para>
/// This provider is registered as scoped, matching the lifecycle of the injected <see cref="IDbConnection"/>.
/// </para>
/// <para>
/// <strong>Connection Lifecycle:</strong>
/// The provider opens the connection if needed when <see cref="BeginTransaction"/> is called.
/// The connection remains open for the duration of the scope and is disposed by the DI container
/// when the scope ends. Multiple calls to <see cref="BeginTransaction"/> within the same scope
/// will reuse the same connection, which is appropriate for nested or sequential transaction scenarios.
/// </para>
/// <para>
/// <strong>Important:</strong>
/// Callers must ensure proper transaction management by calling <see cref="ITransactionScope.Commit"/>
/// or allowing the transaction to roll back via <see cref="ITransactionScope.Dispose"/>.
/// The connection state is managed by the DI container's scope lifecycle.
/// </para>
/// </remarks>
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
        // Ensure connection is open before starting a transaction.
        // If the connection is already open (e.g., from a previous transaction in the same scope),
        // this check ensures we don't attempt to reopen it.
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }

        var transaction = _connection.BeginTransaction();
        return new TransactionScope(transaction);
    }
}
