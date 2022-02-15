using System;
using System.Transactions;
using LocalTransactionScopes;
using SavepointHandlers;

namespace SavepointLocalTransactionScopeObservers;

public class SavepointLocalTransactionScopeObserver: ILocalTransactionScopeObserver
{
    private static readonly Func<TransactionScopeOption, TransactionScope, ILocalTransactionScopeObserver> _transactionScopeObserverFunc = 
        (scopeOption, scope) => new SavepointLocalTransactionScopeObserver(scopeOption, scope);

    public static void Subscribe() => LocalTransactionScope.TryAddObserver(_transactionScopeObserverFunc);
        
    private readonly SavepointHandler _savepointHandler;

    private SavepointLocalTransactionScopeObserver(TransactionScopeOption scopeOption, TransactionScope transactionScope) 
        => _savepointHandler = new SavepointHandler(scopeOption, transactionScope);

    public void OnComplete() => _savepointHandler.Complete();

    public void OnDispose() => _savepointHandler.Dispose();
}