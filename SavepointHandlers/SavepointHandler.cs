using System.Collections.Immutable;
using System.Data;
using System.Threading;
using System.Transactions;

namespace SavepointHandlers
{
    public class SavepointHandler
    {
        private static readonly AsyncLocal<ImmutableStack<SavepointHandler>?> _savepointHandlers = new();

        public static ImmutableStack<SavepointHandler>? SavepointHandlers
        {
            get => _savepointHandlers.Value;
            set => _savepointHandlers.Value = value;
        }

        private readonly SavepointInfo? _savepointInfo;

        public ISavepointExecutor? SavepointExecutor { private get; set; }

        public SavepointHandler(TransactionScopeOption scopeOption)
        {
            var stack = _savepointHandlers.Value ?? ImmutableStack<SavepointHandler>.Empty;
            
            if (scopeOption == TransactionScopeOption.Required)
            {
                if (!stack.IsEmpty)
                {
                    var parent = stack.Peek();
                    
                    var executor = parent.SavepointExecutor;
                    
                    _savepointInfo = executor != null
                        ? new SavepointInfo(executor.Execute(SetSavepoint), executor)
                        : parent._savepointInfo;
                }
            }
            
            _savepointHandlers.Value = stack.Push(this);
        }
        
        public void Complete()
        {
            if (_savepointInfo != null)
                _savepointInfo.IsCompleted = true;
        }
        
        public void Dispose(TransactionScope transactionScope)
        {
            var stack = _savepointHandlers.Value;
            if (stack != null && !stack.IsEmpty)
                _savepointHandlers.Value = stack.Pop();

            if (_savepointInfo != null)
            {
                if (!_savepointInfo.IsCompleted)
                {
                    if (!_savepointInfo.IsRollbacked)
                    {
                        _savepointInfo.Executor.Execute(connection => RollbackToSavepoint(connection, _savepointInfo.Name));
                        _savepointInfo.IsRollbacked = true;
                    }
                    transactionScope.Complete();
                }
            }
        }
        
        private record SavepointInfo(string Name, ISavepointExecutor Executor)
        {
            public bool IsCompleted { get; set; }
            public bool IsRollbacked { get; set; }
        }
        
        private static string SetSavepoint(IDbConnection connection)
        {
            var wasClosed = connection.State == ConnectionState.Closed;
            try
            {
                if (wasClosed)
                    connection.Open();

                using (var command = connection.CreateCommand())
                    return ISavepointAdapter.Current.SetSavepoint(command);
            }
            finally
            {
                if (wasClosed)
                    connection.Close();
            }
        }

        private static void RollbackToSavepoint(IDbConnection connection, string savePointName)
        {
            var wasClosed = connection.State == ConnectionState.Closed;
            try
            {
                if (wasClosed)
                    connection.Open();

                using (var command = connection.CreateCommand()) 
                    ISavepointAdapter.Current.RollbackToSavepoint(command, savePointName);
            }
            finally
            {
                if (wasClosed)
                    connection.Close();
            }
        }
    }
}