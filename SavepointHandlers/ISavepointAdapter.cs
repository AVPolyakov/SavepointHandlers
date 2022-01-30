using System;
using System.Data;

namespace SavepointHandlers
{
    public interface ISavepointAdapter
    {
        private static ISavepointAdapter? _current;
        public static ISavepointAdapter Current
        {
            get
            {
                if (_current == null)
                    throw new Exception($"{nameof(ISavepointAdapter)}.{nameof(Current)} not set");
                
                return _current;
            }
            set => _current = value;
        }

        string SetSavepoint(IDbCommand command);
        
        void RollbackToSavepoint(IDbCommand command, string savePointName);
    }
}