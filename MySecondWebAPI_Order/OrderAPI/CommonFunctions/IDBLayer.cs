//Version 1.2 --22-Oct-2021
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderAPI.CommonFunctions
{
    public interface IDBLayer
    {
        Task<DataTable> GetDataInDataTable(CommandType commandType, string commandText, CancellationToken token);

        Task<DataSet> GetDataInDataSet(CommandType commandType, string commandText, CancellationToken token);

        Task<object> GetScalarValue(CommandType commandType, string commandText, CancellationToken token);

        Task<int> ExecuteNonQuery(CommandType commandType, string commandText, CancellationToken token);
        void AddParameters(string parameterName, DbType dbType, object value, bool isOutputParmeter = false);

        void AddParameters(string parameterName, NpgsqlDbType dbType, object value, bool isOutputParmeter = false);

        void AddParameters(string parameterName, object value, bool isOutputParmeter = false);

        Task<int> InsertUpdateDeleteQuery(CommandType commandType, string commandText, CancellationToken token);

        string GetDataBaseName();

        string GetProviderName();

        void BeginTransaction();

        void CommitTransaction();
        void RollbackTransaction();
    }
}
