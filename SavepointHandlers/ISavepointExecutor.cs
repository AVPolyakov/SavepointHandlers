using System;
using System.Data;

namespace SavepointHandlers
{
    public interface ISavepointExecutor
    {
        TResult Execute<TResult>(Func<IDbConnection, TResult> func);
        
        public void Execute(Action<IDbConnection> action)
        {
            Execute<object?>(connection =>
            {
                action(connection);
                return null;
            });
        }
    }
}