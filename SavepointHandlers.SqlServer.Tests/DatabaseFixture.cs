using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading.Tasks;
using Dapper.Addition;
using Dapper.Addition.SqlServer;
using DbUp;
using Xunit;

namespace SavepointHandlers.SqlServer.Tests
{
    public class DatabaseFixture: IAsyncLifetime
    {
        public IDbExecutor Db { get; }
        public ISavepointExecutor SavepointExecutor { get; }

        public DatabaseFixture()
        {
            Sql.MappingCheckEnabled = true;
            ISqlAdapter.Current = new SqlServerAdapter();
            ISavepointAdapter.Current = new SqlServerSavepointAdapter();
            
            Db = new DbExecutor(ConnectionString);
            SavepointExecutor = new SavepointExecutor(ConnectionString);
        }
        
        private static string ConnectionString => new SqlConnectionStringBuilder(DefaultConnectionString) {InitialCatalog = DatabaseName}.ConnectionString;
        
        private const string DefaultConnectionString = @"Data Source=(local);Initial Catalog=master;Integrated Security=True";
        
        private const string DatabaseName = "SavepointHandlers";
        
        public async Task InitializeAsync()
        {
            var db = new DbExecutor(DefaultConnectionString);
            
            await db.ExecuteAsync(new Sql(@$"
IF EXISTS ( SELECT * FROM sys.databases WHERE name = '{DatabaseName}' )
    DROP DATABASE [{DatabaseName}]

CREATE DATABASE [{DatabaseName}]
"));
            
            var upgrader = DeployChanges.To
                .SqlDatabase(ConnectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .WithTransactionPerScript()
                .LogToConsole()
                .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
                throw new Exception("Database upgrade failed", result.Error);
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}