#nullable enable
using System;
using LocalTransactionScopes;

namespace SavepointHandlers;

public static class LocalTransactionScopeFactory
{
    public static LocalTransactionScope Create(ISavepointExecutor? savepointExecutor)
    {
        var scope = new LocalTransactionScope();
        
        var currentSavepointHandler = SavepointHandler.Current;

        if (currentSavepointHandler == null)
            throw new Exception($"{nameof(SavepointHandler)} is not subscribed to {nameof(LocalTransactionScope)}. " +
                $"Invoke method {nameof(SavepointHandler)}.{nameof(LocalTransactionScopeObserver.Subscribe)} at application start.");
        
        currentSavepointHandler.SavepointExecutor = savepointExecutor;
        
        return scope;
    }
}