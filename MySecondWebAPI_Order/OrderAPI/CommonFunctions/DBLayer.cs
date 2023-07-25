//Version 1.2 --22-Oct-2021
using System;
using System.Data;
using System.Data.Common;
using Npgsql;
using NpgsqlTypes;
using System.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Dynamic;

namespace OrderAPI.CommonFunctions
{
    public static class DataTableExtensions
    {

        public static IEnumerable<dynamic> AsDynamicEnumerable(this DataTable table)
        {
            if (table == null)
            {
                yield break;
            }

            foreach (DataRow row in table.Rows)
            {
                IDictionary<string, object> dRow = new ExpandoObject();

                foreach (DataColumn column in table.Columns)
                {
                    var value = row[column.ColumnName];
                    dRow[column.ColumnName] = Convert.IsDBNull(value) ? null : value;
                }

                yield return dRow;
            }
        }

        public static async Task<int> FillAsync(this DbDataAdapter da, DataSet ds, CancellationToken token)
        {
            int rowcount = 0;
            DbDataReader dr;
            try
            {
                dr = await da.SelectCommand.ExecuteReaderAsync(CommandBehavior.CloseConnection, token);
                DataTable dt;
                do
                {
                    dt = new DataTable();
                    int drFieldcount = dr.FieldCount;
                    DataTable dtColumnSchema = dr.GetSchemaTable();

                    for (int i = 0; i < drFieldcount; i++)
                    {

                        string drFieldname = dr.GetName(i);
                        var fieldtype = dr.GetFieldType(i);

                        if (dt.Columns.Contains(drFieldname))
                        {
                            int index = i;
                            do
                            {
                                index = index + 1;
                            } while (dtColumnSchema.Select("ColumnName = '" + drFieldname + index.ToString() + "'").Length > 0);

                            drFieldname = drFieldname + index.ToString();
                            dtColumnSchema.Rows[i]["ColumnName"] = drFieldname;
                        }

                        var dtColumn = new DataColumn(drFieldname, fieldtype);

                        dt.Columns.Add(dtColumn);
                    }
                    var values = new object[drFieldcount];
                    while (await dr.ReadAsync(token))
                    {
                        dr.GetValues(values);
                        dt.LoadDataRow(values, true);
                        rowcount++;
                    }
                    ds.Tables.Add(dt);
                } while (await dr.NextResultAsync(token));
            }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException();
                else throw ex;
            }
            return rowcount;
        }
    }
    public class DBLayer : IDBLayer
    {
        private string connectionString, providerName;
        private int sqlCommandTimeout = 0;

        DbProviderFactory dbFactory;
        DbConnection dbConn;
        DbParameterCollection dbParameter;
        DbTransaction dbTransaction;

        public DBLayer()
        {
            if (Startup.StatementTimeOut != 0)
            {
                sqlCommandTimeout = Startup.StatementTimeOut + 5;
            }


            connectionString = Startup.ConnectionString;
            providerName = Startup.ProviderName;

            dbFactory = DbProviderFactories.GetFactory(providerName);
            dbConn = CreateConnection();

            dbParameter = getParameterCollection();
            dbTransaction = null;
        }

        ~DBLayer()
        {
            dbParameter = null;
        }

        private DbConnection CreateConnection()
        {
            DbConnection connection = dbFactory.CreateConnection();
            connection.ConnectionString = connectionString;
            return connection;
        }

        private DbCommand CreateCommand(CommandType commandType, string commandText)
        {
            DbCommand dbCommand = dbFactory.CreateCommand();
            dbCommand.CommandType = commandType;
            dbCommand.CommandText = AppendSQLStatements(commandText);
            dbCommand.Connection = dbConn;
            dbCommand.CommandTimeout = sqlCommandTimeout;
            if (dbTransaction != null) dbCommand.Transaction = dbTransaction;

            DbParameter dbParam;
            foreach (DbParameter sbp in dbParameter)
            {
                dbParam = dbFactory.CreateParameter();
                dbParam.ParameterName = sbp.ParameterName;
                dbParam.Direction = sbp.Direction;
                dbParam.DbType = sbp.DbType;
                if (dbParam.GetType().Name == "NpgsqlParameter") dbParam.GetType().GetProperty("NpgsqlDbType").SetValue(dbParam, sbp.GetType().GetProperty("NpgsqlDbType").GetValue(sbp), null);
                if (sbp.Direction == ParameterDirection.Output) dbParam.Size = 100000;
                dbParam.Value = sbp.Value;

                dbCommand.Parameters.Add(dbParam);
            }
            return dbCommand;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionName"></param>
        /// <param name="commandType"></param>
        /// <param name="commandText"></param>
        /// <returns></returns>
        public async Task<DataTable> GetDataInDataTable(CommandType commandType, string commandText, CancellationToken token)
        {
            DataSet ds = await GetDataInDataSet(commandType, commandText, token);
            return ds.Tables[ds.Tables.Count - 1];

        }

        public async Task<DataSet> GetDataInDataSet(CommandType commandType, string commandText, CancellationToken token)
        {
            DbCommand dbCommand = CreateCommand(commandType, commandText);
            DataSet ds = new DataSet();

            try
            {
                if (dbConn.State == ConnectionState.Closed) await dbConn.OpenAsync(token);

                DbDataAdapter da = dbFactory.CreateDataAdapter();
                da.SelectCommand = dbCommand;
                await da.FillAsync(ds, token);

                return ds;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (dbTransaction == null && dbConn.State != ConnectionState.Closed) { await dbConn.CloseAsync(); }
                ClearParameters();
                dbCommand.Dispose(); ds.Dispose();
            }
        }

        /// <summary>
        /// Selects the query.
        /// </summary>
        /// <param name="commandType">Type of the command.</param>
        /// <param name="commandText">The command text.</param>
        /// <param name="parameterValue">The parameter value.</param>
        /// <returns></returns>
        public async Task<object> GetScalarValue(CommandType commandType, string commandText, CancellationToken token)
        {
            DbCommand dbCommand = CreateCommand(commandType, commandText);
            try
            {
                if (dbConn.State == ConnectionState.Closed) await dbConn.OpenAsync(token);

                return await dbCommand.ExecuteScalarAsync(token);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (dbTransaction == null && dbConn.State != ConnectionState.Closed) { dbConn.Close(); }
                ClearParameters();
                dbCommand.Dispose();
            }
        }


        public async Task<int> ExecuteNonQuery(CommandType commandType, string commandText, CancellationToken token)
        {
            DbCommand dbCommand = CreateCommand(commandType, commandText);
            try
            {
                if (dbConn.State == ConnectionState.Closed) await dbConn.OpenAsync(token);
                return await dbCommand.ExecuteNonQueryAsync(token);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (dbTransaction == null && dbConn.State != ConnectionState.Closed) { dbConn.Close(); }
                ClearParameters();
                dbCommand.Dispose();
            }
        }

        /// <summary>
        /// Adds Parameter to AddParameters collection.
        /// </summary>
        /// <param name="parameterName">Name of the parameter</param>
        /// <param name="dbType">Type of parameter</param>
        /// <param name="value">Value to be passed</param>
        public void AddParameters(string parameterName, DbType dbType, object value, bool isOutputParmeter = false)
        {
            DbParameter sbp = dbFactory.CreateParameter();
            sbp.ParameterName = parameterName;
            if (isOutputParmeter)
                sbp.Direction = ParameterDirection.Output;
            else
                sbp.Direction = ParameterDirection.Input;
            sbp.DbType = dbType;
            sbp.Value = value;
            dbParameter.Add(sbp);
        }

        /// <summary>
        /// Adds Parameter to AddParameters collection Exclusive for NpgSQL.
        /// </summary>
        /// <param name="parameterName">Name of the parameter</param>
        /// <param name="dbType">Type of parameter</param>
        /// <param name="value">Value to be passed</param>
        public void AddParameters(string parameterName, NpgsqlDbType dbType, object value, bool isOutputParmeter = false)
        {
            NpgsqlParameter sbp = new NpgsqlParameter()
            {
                ParameterName = parameterName,
                Direction = isOutputParmeter ? ParameterDirection.Output : ParameterDirection.Input,
                NpgsqlDbType = dbType,
                Value = value
            };
            dbParameter.Add(sbp);
        }
        public void AddParameters(string parameterName, object value, bool isOutputParmeter = false)
        {
            NpgsqlParameter sbp = new NpgsqlParameter()
            {
                ParameterName = parameterName,
                Direction = isOutputParmeter ? ParameterDirection.Output : ParameterDirection.Input,
                Value = value
            };
            dbParameter.Add(sbp);
        }
        public async Task<int> InsertUpdateDeleteQuery(CommandType commandType, string commandText, CancellationToken token)
        {
            DbCommand dbCommand = CreateCommand(commandType, commandText);

            try
            {
                if (dbConn.State == ConnectionState.Closed) await dbConn.OpenAsync(token);
                return await dbCommand.ExecuteNonQueryAsync(token);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (dbTransaction == null && dbConn.State != ConnectionState.Closed) { dbConn.Close(); }
                ClearParameters();
                dbCommand.Dispose();
            }
        }

        private void ClearParameters()
        {
            dbParameter = null;
            dbParameter = getParameterCollection();
        }

        private DbParameterCollection getParameterCollection()
        {
            DbParameterCollection paramCollection = null;
            DbCommand cmd = dbFactory.CreateCommand();
            DbParameter prm = (DbParameter)cmd.CreateParameter();
            paramCollection = (DbParameterCollection)cmd.Parameters;

            return paramCollection;
        }

        private string AppendSQLStatements(string commandText)
        {
            string Appendcmd = string.Empty;
            if (Startup.StatementTimeOut != 0)
                Appendcmd = "SET statement_timeout = '" + Convert.ToString(Startup.StatementTimeOut) + "s'; ";
            if (Startup.EnableReadUnCommit == true)
                Appendcmd = Appendcmd + " SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; ";
            return Appendcmd + commandText;
        }

        public string GetDataBaseName()
        {
            return dbConn.Database;
        }

        public string GetProviderName()
        {
            return providerName;
        }

        /// <summary>
        /// Sets a value indicating whether [begin transaction].
        /// </summary>
        /// <value><c>true</c> if [begin transaction]; otherwise, <c>false</c>.</value>
        public void BeginTransaction()
        {
            if (dbConn.State == ConnectionState.Closed) dbConn.Open();
            dbTransaction = dbConn.BeginTransaction();
        }

        /// <summary>
        /// Sets a value indicating whether [commit transaction].
        /// </summary>
        /// <value><c>true</c> if [commit transaction]; otherwise, <c>false</c>.</value>
        public void CommitTransaction()
        {
            if (dbConn.State != ConnectionState.Closed)
            {
                dbTransaction.Commit();
                dbConn.Close();
                dbTransaction = null;
            }
        }

        /// <summary>
        /// Sets a value indicating whether [rollback transaction].
        /// </summary>
        /// <value><c>true</c> if [rollback transaction]; otherwise, <c>false</c>.</value>
        public void RollbackTransaction()
        {
            if (dbConn.State != ConnectionState.Closed)
            {
                dbTransaction.Rollback();
                dbConn.Close();
                dbTransaction = null;
            }
        }
    }

}