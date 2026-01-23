namespace FestConnect.DataAccess.Abstractions;

/// <summary>
/// Provides database transaction management capabilities.
/// </summary>
public interface IDbTransactionProvider
{
    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <returns>A transaction scope that must be committed or disposed.</returns>
    ITransactionScope BeginTransaction();
}
