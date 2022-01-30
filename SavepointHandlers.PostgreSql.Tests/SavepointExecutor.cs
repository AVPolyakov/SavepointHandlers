using System;
using System.Data;
using Npgsql;

namespace SavepointHandlers.PostgreSql.Tests
{
    public class SavepointExecutor : ISavepointExecutor
    {
        private readonly string _connectionString;

        public SavepointExecutor(string connectionString)
        {
            _connectionString = connectionString;
        }
        
        public TResult Execute<TResult>(Func<IDbConnection, TResult> func)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
                return func(connection);
        }
    }
}