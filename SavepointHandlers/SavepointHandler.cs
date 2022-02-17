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
        private bool _isDisposed;

        public ISavepointExecutor? SavepointExecutor { private get; set; }

        public SavepointHandler(TransactionScopeOption scopeOption)
        {
            var parent = Current;

            _parent = parent;
            _savepointScopeInfo = GetSavepointScopeInfo(scopeOption, parent);
            
            Current = this;
        }

        private static SavepointScopeInfo? GetSavepointScopeInfo(TransactionScopeOption scopeOption, SavepointHandler? parent)
        {
            if (scopeOption == TransactionScopeOption.Required)
            {
                if (parent != null)
                {
                    var parentExecutor = parent.SavepointExecutor;

                    return parentExecutor != null
                        ? new SavepointScopeInfo(SetSavepoint(parentExecutor), parentExecutor)
                        : parent._savepointScopeInfo != null
                            ? new SavepointScopeInfo(parent._savepointScopeInfo.Name, parent._savepointScopeInfo.Executor)
                            : null;
                }
            }
                
            return null;
        }

        public void Complete()
        {
            if (_savepointScopeInfo != null)
                _savepointScopeInfo.IsCompleted = true;
        }
        
        public void Dispose(TransactionScope transactionScope)
        {
            if (_savepointScopeInfo != null)
            {
                if (!_savepointScopeInfo.IsCompleted)
                {
                    RollbackToSavepoint(_savepointScopeInfo.Executor, _savepointScopeInfo.Name);
                    transactionScope.Complete();
                }
            }
            
            if (Current == this)
            {
                var parent = _parent;
                while (parent != null && parent._isDisposed)
                {
                    parent = parent._parent;
                }
                Current = _parent;
            }

            _isDisposed = true;
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