using System.Transactions;

namespace LocalTransactionScopes;

public interface ILocalTransactionScopeObserver
{
    void OnCreated(TransactionScopeOption scopeOption, TransactionScope transactionScope);
    
    void OnComplete(TransactionScope transactionScope);
    
    void OnDispose(TransactionScope transactionScope);
}