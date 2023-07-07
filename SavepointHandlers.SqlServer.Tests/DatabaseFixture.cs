using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Threading.Tasks;
using Dapper.Addition;
using Dapper.Addition.SqlServer;
using DbUp;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SavepointHandlers.SqlServer.Tests
{
    public sealed class DatabaseFixture: IAsyncLifetime, IDisposable
    {
        public IHost Host { get; }

        public IDbExecutor Db { get; }
        public ISavepointExecutor SavepointExecutor { get; }

        public DatabaseFixture()
        {
            Sql.MappingCheckEnabled = true;
            ISqlAdapter.Current = new SqlServerAdapter();
            ISavepointAdapter.Current = new SqlServerSavepointAdapter();
            
            Db = new DbExecutor(ConnectionString);
            SavepointExecutor = new SavepointExecutor(ConnectionString);
            
            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

            builder.Services.AddSingleton<IClientService, ClientService>();

            Host = builder.Build();
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

        public void Dispose() => Host.Dispose();
    }
}