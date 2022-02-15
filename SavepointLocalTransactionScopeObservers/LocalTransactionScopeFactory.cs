using LocalTransactionScopes;
using SavepointHandlers;

namespace SavepointLocalTransactionScopeObservers;

public static class LocalTransactionScopeFactory
{
    public static LocalTransactionScope Create(ISavepointExecutor? savepointExecutor)
    {
        var scope = new LocalTransactionScope();
        SavepointHandler.CurrentSavepointExecutor = savepointExecutor;
        return scope;
    }
}