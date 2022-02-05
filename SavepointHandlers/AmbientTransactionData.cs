using System.Collections.Immutable;
using System.Transactions;

namespace SavepointHandlers
{
    public class AmbientTransactionData
    {
        private readonly Transaction? _transaction;
        private readonly SavepointHandler? _savepointHandler;

        private AmbientTransactionData(Transaction? transaction, SavepointHandler? savepointHandler)
        {
            _transaction = transaction;
            _savepointHandler = savepointHandler;
        }

        public static AmbientTransactionData Current
        {
            get => new(Transaction.Current, SavepointHandler.Current);
            set
            {
                Transaction.Current = value._transaction;
                SavepointHandler.Current = value._savepointHandler;
            }
        }
    }
}