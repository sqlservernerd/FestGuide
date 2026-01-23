namespace FestConnect.DataAccess.Tests;

/// <summary>
/// Placeholder test to verify the test project is configured correctly.
/// Real repository tests will be added when database schema is fully implemented.
/// </summary>
public class RepositoryConfigurationTests
{
    [Fact]
    public void DataAccessTestProject_IsConfiguredCorrectly()
    {
        // This test verifies the test project compiles and runs
        Assert.True(true);
    }

    [Fact(Skip = "Requires Docker for Testcontainers - run manually")]
    public async Task SqlContainer_CanBeCreated()
    {
        // This test verifies Testcontainers can create a SQL Server container
        // Skip by default as it requires Docker
        await using var testBase = new TestRepositoryBase();
        await testBase.InitializeAsync();
        
        Assert.NotNull(testBase.GetConnectionString());
        
        await testBase.DisposeAsync();
    }

    private class TestRepositoryBase : RepositoryTestBase
    {
        public string GetConnectionString() => ConnectionString;
    }
}
