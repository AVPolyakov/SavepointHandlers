using System;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace SavepointHandlers.PostgreSql.Tests
{
    public sealed class LocalTransactionScope: IDisposable
    {
        private readonly TransactionScope _transactionScope;
        private readonly SavepointHandler _savepointHandler;

        public LocalTransactionScope(TransactionScopeOption scopeOption = TransactionScopeOption.Required)
        {
            _transactionScope = new TransactionScope(
                scopeOption,
                new TransactionOptions{IsolationLevel = IsolationLevel.ReadCommitted},
                TransactionScopeAsyncFlowOption.Enabled);
            
            _savepointHandler = new SavepointHandler(scopeOption);
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