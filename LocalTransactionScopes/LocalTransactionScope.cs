using System;
using System.Collections.Concurrent;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace LocalTransactionScopes
{
    public sealed class LocalTransactionScope: IDisposable
    {
        private static readonly ConcurrentDictionary<ILocalTransactionScopeObserver, object?> _observers = new();

        public static void TryAddObserver(ILocalTransactionScopeObserver observer) 
            => _observers.TryAdd(observer, null);

        private readonly TransactionScope _transactionScope;
        
        public LocalTransactionScope(TransactionScopeOption scopeOption = TransactionScopeOption.Required)
        {
            _transactionScope = new TransactionScope(
                scopeOption,
                new TransactionOptions{IsolationLevel = IsolationLevel.ReadCommitted},
                TransactionScopeAsyncFlowOption.Enabled);
            
            foreach (var observer in _observers.Keys)
                observer.OnCreated(scopeOption, _transactionScope);
        }
        
        public void Complete()
        {
            foreach (var observer in _observers.Keys)
                observer.OnComplete(_transactionScope);
            
            _transactionScope.Complete();
        }

        public void Dispose()
        {
            foreach (var observer in _observers.Keys)
                observer.OnDispose(_transactionScope);

            _transactionScope.Dispose();
        }
    }
}