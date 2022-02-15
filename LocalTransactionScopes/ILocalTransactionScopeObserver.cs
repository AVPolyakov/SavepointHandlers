namespace LocalTransactionScopes;

public interface ILocalTransactionScopeObserver
{
    void OnComplete();
    void OnDispose();
}