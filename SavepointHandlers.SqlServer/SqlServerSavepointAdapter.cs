using System;
using System.Data;
using SavepointHandlers;

namespace SavepointHandlers.SqlServer
{
    public class SqlServerSavepointAdapter : ISavepointAdapter
    {
        public string SetSavepoint(IDbCommand command)
        {
            var savePointName = Guid.NewGuid().ToString("N");
            
            command.CommandText = "SAVE TRANSACTION @SavePointName";
            
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@SavePointName";
            parameter.Value = savePointName;
            command.Parameters.Add(parameter);
            
            command.ExecuteNonQuery();
            
            return savePointName;
        }

        public void RollbackToSavepoint(IDbCommand command, string savePointName)
        {
            command.CommandText = "ROLLBACK TRANSACTION @SavePointName";
            
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@SavePointName";
            parameter.Value = savePointName;
            command.Parameters.Add(parameter);
            
            command.ExecuteNonQuery();
        }
    }
}