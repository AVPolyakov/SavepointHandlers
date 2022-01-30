using Xunit;

namespace SavepointHandlers.PostgreSql.Tests
{
    [CollectionDefinition(nameof(FixtureCollection))]
    public class FixtureCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}