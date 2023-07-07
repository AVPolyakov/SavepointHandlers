using System;
using System.Threading.Tasks;
using Dapper.Addition;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SavepointHandlers.SqlServer.Tests
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

            _transactionScope = new LocalTransactionScope { SavepointExecutor = databaseFixture.SavepointExecutor };

            AmbientTransactionData = AmbientTransactionData.Current;
        }

        public async Task InitializeAsync()
        {
            await _db.InsertAsync(new Client {Id = 1, Name = "Name1"});
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
            
            var clientService = databaseFixture.Host.Services.GetRequiredService<IClientService>();

            AmbientTransactionData.Current = fixture.AmbientTransactionData;
            
            _transactionScope = new LocalTransactionScope {SavepointExecutor = databaseFixture.SavepointExecutor};
        }
        
        public void Dispose() => _transactionScope.Dispose();

        [Fact]
        public async Task UpdateToName2_Success()
        {
            {
                var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @Id", new { Id = 1 });
                Assert.Equal("Name1", name);
            }

            await _db.ExecuteAsync("UPDATE Clients SET Name = @Name WHERE Id = @Id", new { Name = "Name2", Id = 1 });

            {
                var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @Id", new { Id = 1 });
                Assert.Equal("Name2", name);
            }
        }

        [Fact]
        public async Task UpdateToName3_Success()
        {
            {
                var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @Id", new { Id = 1 });
                Assert.Equal("Name1", name);
            }

            await _db.ExecuteAsync("UPDATE Clients SET Name = @Name WHERE Id = @Id", new { Name = "Name3", Id = 1 });

            {
                var name = await _db.QuerySingleAsync<string>("SELECT Name FROM Clients WHERE Id = @Id", new { Id = 1 });
                Assert.Equal("Name3", name);
            }
        }
    }
}