using System.Data;
using System.Threading;
using System.Transactions;

namespace SavepointHandlers
{
    public class SavepointHandler
    {
        private static readonly AsyncLocal<SavepointHandler?> _current = new();

        public static SavepointHandler? Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }

        private readonly SavepointInfo? _savepointInfo;
        private readonly SavepointHandler? _parent;

        public ISavepointExecutor? SavepointExecutor { private get; set; }

        public SavepointHandler(TransactionScopeOption scopeOption)
        {
            var parent = _current.Value;
            
            if (scopeOption == TransactionScopeOption.Required)
            {
                if (parent != null)
                {
                    var executor = parent.SavepointExecutor;

                    _savepointInfo = executor != null
                        ? new SavepointInfo(executor.Execute(SetSavepoint), executor)
                        : parent._savepointInfo;
                }
            }

            _parent = parent;
            _current.Value = this;
        }
        
        public void Complete()
        {
            if (_savepointInfo != null)
                _savepointInfo.IsCompleted = true;
        }
        
        public void Dispose(TransactionScope transactionScope)
        {
            _current.Value = _parent;

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