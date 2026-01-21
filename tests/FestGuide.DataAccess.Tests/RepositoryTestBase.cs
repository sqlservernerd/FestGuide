using Testcontainers.MsSql;

namespace FestGuide.DataAccess.Tests;

/// <summary>
/// Base class for repository integration tests using SQL Server Testcontainers.
/// Provides a real SQL Server database for testing repository implementations.
/// </summary>
public abstract class RepositoryTestBase : IAsyncLifetime
{
    protected MsSqlContainer? SqlContainer { get; private set; }
    protected string ConnectionString => SqlContainer?.GetConnectionString() 
        ?? throw new InvalidOperationException("SQL container not initialized");

    public async Task InitializeAsync()
    {
        SqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();

        await SqlContainer.StartAsync();

        // Apply database schema here when needed
        await SetupDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (SqlContainer != null)
        {
            await SqlContainer.DisposeAsync();
        }
    }

    /// <summary>
    /// Override this method to set up the database schema for tests.
    /// </summary>
    protected virtual Task SetupDatabaseAsync() => Task.CompletedTask;
}
