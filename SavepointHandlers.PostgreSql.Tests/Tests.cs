using System;
using System.Threading.Tasks;
using Dapper.Addition;
using Dapper.Addition.PostgreSql.Tests;
using LocalTransactionScopes;
using SavepointLocalTransactionScopeObservers;
using Xunit;

namespace SavepointHandlers.PostgreSql.Tests
{
    [Collection(nameof(FixtureCollection))]
    public class TestFixture : IDisposable, IAsyncLifetime 
    {
        private readonly IDbExecutor _db;
        private readonly LocalTransactionScope _transactionScope;
        public AmbientTransactionData AmbientTransactionData { get; }

        public TestFixture(DatabaseFixture databaseFixture)
        {
            _db = databaseFixture.Db;

            _transactionScope = LocalTransactionScopeFactory.Create(databaseFixture.SavepointExecutor);

            AmbientTransactionData = AmbientTransactionData.Current;
        }

        public async Task InitializeAsync()
        {
            await _db.InsertAsync(new Client {Id = 2, Name = "Name1"});
        }

        public Task DisposeAsync() => Task.CompletedTask;
        
        public void Dispose() => _transactionScope.Dispose();
    }
    
    [Collection(nameof(FixtureCollection))]
    public class Tests: IClassFixture<TestFixture>, IDisposable
    {
        private readonly IDbExecutor _db;
        private readonly LocalTransactionScope _transactionScope;
        
        public Tests(TestFixture fixture,
            DatabaseFixture databaseFixture)
        {
            _db = databaseFixture.Db;

            AmbientTransactionData.Current = fixture.AmbientTransactionData;
            
            _transactionScope = LocalTransactionScopeFactory.Create(databaseFixture.SavepointExecutor);
        }
        
        public void Dispose() => _transactionScope.Dispose();

        [Fact]
        public async Task UpdateToName2_Success()
        {
            {
                var name = await _db.QuerySingleAsync<string>("SELECT name FROM clients WHERE id = @Id", new { Id = 2 });
                Assert.Equal("Name1", name);
            }

            await _db.ExecuteAsync("UPDATE clients SET name = @Name WHERE id = @Id", new { Name = "Name2", Id = 2 });

            {
                var name = await _db.QuerySingleAsync<string>("SELECT name FROM clients WHERE id = @Id", new { Id = 2 });
                Assert.Equal("Name2", name);
            }
        }

        [Fact]
        public async Task UpdateToName3_Success()
        {
            {
                var name = await _db.QuerySingleAsync<string>("SELECT name FROM clients WHERE id = @Id", new { Id = 2 });
                Assert.Equal("Name1", name);
            }

            await _db.ExecuteAsync("UPDATE clients SET name = @Name WHERE id = @Id", new { Name = "Name3", Id = 2 });

            {
                var name = await _db.QuerySingleAsync<string>("SELECT name FROM clients WHERE id = @Id", new { Id = 2 });
                Assert.Equal("Name3", name);
            }
        }
    }
}