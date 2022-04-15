using System.Threading.Tasks;
using Dapper.Addition;
using Xunit;

namespace SavepointHandlers.SqlServer.Tests
{
    [Collection(nameof(FixtureCollection))]
    public class SavepointHandlerTests
    {
        private readonly DatabaseFixture _databaseFixture;
        private readonly IDbExecutor _db;

        public SavepointHandlerTests(DatabaseFixture databaseFixture)
        {
            _databaseFixture = databaseFixture;
            _db = databaseFixture.Db;
        }

        [Fact]
        public async Task Nested_Rollback_Success()
        {
            using (new LocalTransactionScope { SavepointExecutor = _databaseFixture.SavepointExecutor })
            {
                await _db.InsertAsync(new Client {Id = 2, Name = "Name1"});
                
                using (new LocalTransactionScope())
                {
                    using (new LocalTransactionScope())
                    {
                    }
                    
                    await _db.ExecuteAsync("UPDATE Clients SET Name = @Name WHERE Id = @Id", new { Name = "Name2", Id = 2 });
                }
                
                {
                    var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @Id", new { Id = 2 });
                    Assert.Equal("Name1", name);
                }
            }
        }
        
        [Fact]
        public async Task Nested_Complete_Success()
        {
            using (new LocalTransactionScope { SavepointExecutor = _databaseFixture.SavepointExecutor })
            {
                await _db.InsertAsync(new Client {Id = 2, Name = "Name1"});
                
                using (new LocalTransactionScope())
                {
                    using (var scope = new LocalTransactionScope())
                    {
                        scope.Complete();
                    }

                    using (new LocalTransactionScope())
                    {
                        await _db.ExecuteAsync("UPDATE Clients SET Name = @Name WHERE Id = @Id", new { Name = "Name2", Id = 2 });
                    }
                }
                
                {
                    var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @Id", new { Id = 2 });
                    Assert.Equal("Name1", name);
                }
            }
        }
    }
}