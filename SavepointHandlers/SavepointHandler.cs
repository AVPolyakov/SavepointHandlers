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

        private readonly SavepointScopeInfo? _savepointScopeInfo;
        private readonly SavepointHandler? _parent;

        public ISavepointExecutor? SavepointExecutor { private get; set; }

        public SavepointHandler(TransactionScopeOption scopeOption)
        {
            var parent = _current.Value;
            
            if (scopeOption == TransactionScopeOption.Required)
            {
                if (parent != null)
                {
                    var parentExecutor = parent.SavepointExecutor;

                    _savepointScopeInfo = parentExecutor != null 
                        ? new SavepointScopeInfo(SetSavepoint(parentExecutor), parentExecutor) 
                        : parent._savepointScopeInfo != null 
                            ? new SavepointScopeInfo(parent._savepointScopeInfo.Name, parent._savepointScopeInfo.Executor) 
                            : null;
                }
            }

            _parent = parent;
            _current.Value = this;
        }
        
        public void Complete()
        {
            if (_savepointScopeInfo != null)
                _savepointScopeInfo.IsCompleted = true;
        }
        
        public void Dispose(TransactionScope transactionScope)
        {
            _current.Value = _parent;

            if (_savepointScopeInfo != null)
            {
                if (!_savepointScopeInfo.IsCompleted)
                {
                    RollbackToSavepoint(_savepointScopeInfo.Executor, _savepointScopeInfo.Name);
                    transactionScope.Complete();
                }
            }
        }
        
        private record SavepointScopeInfo(string Name, ISavepointExecutor Executor)
        {
            public bool IsCompleted { get; set; }
        }
        
        private static string SetSavepoint(ISavepointExecutor executor)
        {
            return executor.Execute(connection =>
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
            });
        }

        private static void RollbackToSavepoint(ISavepointExecutor executor, string savePointName)
        {
            executor.Execute(connection =>
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
            });
        }
    }
}