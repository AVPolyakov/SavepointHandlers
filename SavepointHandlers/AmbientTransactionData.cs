using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace SavepointHandlers
{
    public class AmbientTransactionData
    {
        private readonly Transaction? _transaction;
        private readonly SavepointHandler? _savepointHandler;
        private readonly List<Action> _actions;

        private static readonly ConcurrentDictionary<Func<Action>, object?> _ambientDataFuncs = new();

        public static void Add(Func<Action> func)
        {
            _ambientDataFuncs.TryAdd(func, null);
        }
        
        private AmbientTransactionData(Transaction? transaction, SavepointHandler? savepointHandler, List<Action> actions)
        {
            _transaction = transaction;
            _savepointHandler = savepointHandler;
            _actions = actions;
        }

        public static AmbientTransactionData Current
        {
            get
            {
                var actions = _ambientDataFuncs.Keys.Select(func => func()).ToList();
                return new AmbientTransactionData(Transaction.Current, SavepointHandler.Current, actions);
            }
            set
            {
                Transaction.Current = value._transaction;
                SavepointHandler.Current = value._savepointHandler;
                
                foreach (var action in value._actions) 
                    action();
            }
        }
    }
}