using System;
using System.Collections.Concurrent;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace SavepointHandlers
{
    public sealed class LocalTransactionScope: IDisposable
    {
        internal static readonly ConcurrentDictionary<ITransactionObserver, object?> TransactionObservers = new();

        public static void AddTransactionObserver(ITransactionObserver transactionObserver)
        {
            TransactionObservers.TryAdd(transactionObserver, null);
        }

        private readonly TransactionScope _transactionScope;
        private readonly SavepointHandler _savepointHandler;

        public LocalTransactionScope(TransactionScopeOption scopeOption = TransactionScopeOption.Required)
        {
            var transaction = Transaction.Current;

            _transactionScope = new TransactionScope(
                scopeOption,
                new TransactionOptions{IsolationLevel = IsolationLevel.ReadCommitted},
                TransactionScopeAsyncFlowOption.Enabled);
            
            _savepointHandler = new SavepointHandler(scopeOption);

            if (Transaction.Current != transaction)
                foreach (var transactionObserver in TransactionObservers.Keys)
                    transactionObserver.OnBegin();
        }
        
        public void Complete()
        {
            _savepointHandler.Complete();
            
            _transactionScope.Complete();
        }

        public void Dispose()
        {
            _savepointHandler.Dispose(_transactionScope);

            _transactionScope.Dispose();
        }

        public ISavepointExecutor? SavepointExecutor
        {
            set => _savepointHandler.SavepointExecutor = value;
        }
    }
}