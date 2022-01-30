using System;
using System.Reflection;
using System.Threading.Tasks;
using Dapper;
using Dapper.Addition;
using Dapper.Addition.PostgreSql;
using Dapper.Addition.PostgreSql.Tests;
using DbUp;
using Npgsql;
using Xunit;

namespace SavepointHandlers.PostgreSql.Tests
{
    public class DatabaseFixture: IAsyncLifetime
    {
        public IDbExecutor Db { get; }
        public ISavepointExecutor SavepointExecutor { get; }

        public DatabaseFixture()
        {
            Sql.MappingCheckEnabled = true;
            ISqlAdapter.Current = new PostgreSqlAdapter();
            ISavepointAdapter.Current = new PostgreSqlSavepointAdapter();
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            
            Db = new DbExecutor(ConnectionString);
            SavepointExecutor = new SavepointExecutor(ConnectionString);
        }
        
        private static string ConnectionString => new NpgsqlConnectionStringBuilder(DefaultConnectionString) {Database = DatabaseName}.ConnectionString;
        
        private const string DefaultConnectionString = @"Server=127.0.0.1;Port=5432;Database=postgres;User Id=postgres;Password=qwe123456;";
        
        private const string DatabaseName = "dapper_addition";

        public async Task InitializeAsync()
        {
            var db = new DbExecutor(DefaultConnectionString);

            var singleOrDefault = await db.QuerySingleOrDefaultAsync<string?>(new Sql(@$"
SELECT datname
FROM pg_catalog.pg_database 
WHERE datname = '{DatabaseName}'"));

            if (singleOrDefault != null)
                await db.ExecuteAsync(new Sql($"DROP DATABASE {DatabaseName}"));

            await db.ExecuteAsync(new Sql($"CREATE DATABASE {DatabaseName}"));

            var upgrader = DeployChanges.To
                .PostgresqlDatabase(ConnectionString)
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