using System.Transactions;
using LocalTransactionScopes;

namespace SavepointHandlers;

public class LocalTransactionScopeObserver : ILocalTransactionScopeObserver
{
    private static readonly LocalTransactionScopeObserver _transactionScopeObserverFunc = new();

    public static void Subscribe() => LocalTransactionScope.TryAddObserver(_transactionScopeObserverFunc);
        
    public void OnCreated(TransactionScopeOption scopeOption, TransactionScope transactionScope)
    {
        SavepointHandler.CreateCurrent(scopeOption);
    }

    public void OnComplete(TransactionScope transactionScope)
    {
        SavepointHandler.Current?.Complete();
    }

    public void OnDispose(TransactionScope transactionScope)
    {
        SavepointHandler.Current?.Dispose(transactionScope);
    }
}