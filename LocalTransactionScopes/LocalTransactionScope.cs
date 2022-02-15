using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace LocalTransactionScopes
{
    public sealed class LocalTransactionScope: IDisposable
    {
        private static readonly ConcurrentDictionary<Func<TransactionScopeOption, TransactionScope, ILocalTransactionScopeObserver>, object?> _observerFuncs = new();

        public static void TryAddObserver(Func<TransactionScopeOption, TransactionScope, ILocalTransactionScopeObserver> observerFunc) 
            => _observerFuncs.TryAdd(observerFunc, null);

        private readonly TransactionScope _transactionScope;
        private readonly List<ILocalTransactionScopeObserver> _observers;
        
        public LocalTransactionScope(TransactionScopeOption scopeOption = TransactionScopeOption.Required)
        {
            _transactionScope = new TransactionScope(
                scopeOption,
                new TransactionOptions{IsolationLevel = IsolationLevel.ReadCommitted},
                TransactionScopeAsyncFlowOption.Enabled);
            
            _observers = _observerFuncs.Keys.Select(observerFunc => observerFunc(scopeOption, _transactionScope)).ToList();
        }
        
        public void Complete()
        {
            foreach (var observer in _observers)
                observer.OnComplete();
            
            _transactionScope.Complete();
        }

        public void Dispose()
        {
            foreach (var observer in _observers)
                observer.OnDispose();

            _transactionScope.Dispose();
        }
    }
}