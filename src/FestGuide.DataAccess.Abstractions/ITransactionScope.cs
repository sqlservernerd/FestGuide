using System.Data;

namespace FestGuide.DataAccess.Abstractions;

/// <summary>
/// Represents a database transaction scope that can be committed or rolled back.
/// </summary>
public interface ITransactionScope : IDisposable
{
    /// <summary>
    /// Gets the underlying database transaction.
    /// </summary>
    IDbTransaction Transaction { get; }

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    void Commit();

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    void Rollback();
}
