namespace SavepointHandlers;

public interface ITransactionObserver
{
    void OnBegin();
    void OnSetSavepoint(string savepointName);
    void OnRollbackToSavepoint(string savepointName);
}