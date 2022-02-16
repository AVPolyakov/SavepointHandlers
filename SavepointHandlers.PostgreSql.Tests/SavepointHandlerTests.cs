using System.Threading.Tasks;
using Dapper.Addition;
using Dapper.Addition.PostgreSql.Tests;
using LocalTransactionScopes;
using Xunit;

namespace SavepointHandlers.PostgreSql.Tests
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
            using (LocalTransactionScopeFactory.Create(_databaseFixture.SavepointExecutor))
            {
                await _db.InsertAsync(new Client {Id = 2, Name = "Name1"});
                
                using (new LocalTransactionScope())
                {
                    using (new LocalTransactionScope())
                    {
                    }
                    
                    await _db.ExecuteAsync("UPDATE clients SET name = @Name WHERE id = @Id", new { Name = "Name2", Id = 2 });
                }
                
                {
                    var name = await _db.QuerySingleAsync<string>("SELECT name FROM clients WHERE id = @Id", new { Id = 2 });
                    Assert.Equal("Name1", name);
                }
            }
        }
        
        [Fact]
        public async Task Nested_Complete_Success()
        {
            using (LocalTransactionScopeFactory.Create(_databaseFixture.SavepointExecutor))
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
                        await _db.ExecuteAsync("UPDATE clients SET name = @Name WHERE id = @Id", new { Name = "Name2", Id = 2 });
                    }
                }
                
                {
                    var name = await _db.QuerySingleAsync<string>("SELECT name FROM clients WHERE id = @Id", new { Id = 2 });
                    Assert.Equal("Name1", name);
                }
            }
        }
    }
}