using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Transactions;
using EntLibContrib.Data.SQLite;
using Microsoft.Practices.EnterpriseLibrary.Data;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;

namespace JHelper.DB
{
    public static class DbHelper
    {
        public static bool IsDebug = false;
        public static Dictionary<string, Database> SqlServerDbPool = new Dictionary<string, Database>();
        public static Database GetDatabase(string name = "con", string connectionString = "")
        {
            if (SqlServerDbPool.ContainsKey(name))
            {
                return SqlServerDbPool[name];
            }
            else
            {
                Database sqlServerDb;
                if (connectionString.IndexOf("Version", StringComparison.Ordinal) == -1)
                {
                    sqlServerDb = new SqlDatabase(connectionString);
                }
                else
                {
                    sqlServerDb = new SQLiteDatabase(connectionString);
                }
                SqlServerDbPool.Add(name, sqlServerDb);
                return sqlServerDb;
            }
            
        }

        public static TransactionScope GetTransactionScope(TransactionScopeOption scopeOption = TransactionScopeOption.Required, int seconds = 30)
        {
            return new TransactionScope(scopeOption, TimeSpan.FromSeconds(seconds));
        }

        public static int ExecuteNonQuery(string sql, string name = "con")
        {
            return GetDatabase(name).ExecuteNonQuery(CommandType.Text, sql);
        }

        public static object ExecuteScalar(string sql, string name = "con")
        {
            return GetDatabase(name).ExecuteScalar(CommandType.Text, sql);
        }

        public static DataSet ExecuteDataSet(string sql, string name = "con")
        {
            return GetDatabase(name).ExecuteDataSet(CommandType.Text, sql);
        }

        public static IDataReader ExecuteReader(string sql, string name = "con")
        {
                    return GetDatabase(name).ExecuteReader(CommandType.Text, sql);
        }

        public static DataTable ExecuteDataTable(string sql, string name = "con")
        {
            return GetDatabase(name).ExecuteDataSet(CommandType.Text, sql).Tables[0];
        }


        public static int ExecuteNonQuery(DbCommand dbCommand, string name = "con")
        {
            return GetDatabase(name).ExecuteNonQuery(dbCommand);
        }

        public static object ExecuteScalar(DbCommand dbCommand, string name = "con")
        {
            return GetDatabase(name).ExecuteScalar(dbCommand);
        }

        public static DataSet ExecuteDataSet(DbCommand dbCommand, string name = "con")
        {
            return GetDatabase(name).ExecuteDataSet(dbCommand);
        }

        public static IDataReader ExecuteReader(DbCommand dbCommand, string name = "con")
        {
            return GetDatabase(name).ExecuteReader(dbCommand);
        }

        public static DataTable ExecuteDataTable(DbCommand dbCommand, string name = "con")
        {
            return GetDatabase(name).ExecuteDataSet(dbCommand).Tables[0];
        }

        public static DataTable GetTable(string table, string name = "con")
        {
            return ExecuteDataSet(SimpleSqlCreater.Select(table).ToString(), name).Tables[0];
        }

        public static bool TestConnection(string name = "con")
        {
            DbConnection connect = null;
            try
            {
                connect = GetDatabase(name).CreateConnection();
                connect.Open();
            }
            finally
            {
                if (connect != null) connect.Close();
            }
            return true;
        }
        public static T GetModel<T>(string sql, string name = "con")
        {
            T model = (T)Activator.CreateInstance(typeof(T));
            DataTable dt = ExecuteDataTable(sql, name);
            if (dt.Rows.Count > 0)
            {
                model = GetDataRowToModel<T>(dt.Rows[0], typeof(T));
            }
            return model;
        }
        public static List<T> GetList<T>(string sql, string name = "con")
        {
            List<T> list = new List<T>();
            DataTable dt = ExecuteDataTable(sql, name);
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                Type tm = typeof (T);
                var model = GetDataRowToModel<T>(dt.Rows[i], tm);
                list.Add(model);
            }
            return list;
        }

        public static List<T> GetDataTableToList<T>(DataTable dt)
        {
            List<T> list = new List<T>();
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                Type tm = typeof(T);
                var model = GetDataRowToModel<T>(dt.Rows[i], tm);
                list.Add(model);
            }
            return list;
        }

        public static T GetDataRowToModel<T>(DataRow dr, Type tm)
        {
            T model = (T) Activator.CreateInstance(tm);
            PropertyInfo[] pis = tm.GetProperties();
            foreach (PropertyInfo pi in pis)
            {
                if (pi.CanWrite)
                {
                    string propertyName = pi.Name;
                    if (dr.RowState != DataRowState.Detached && !dr.Table.Columns.Contains(propertyName))
                    {
                        continue;
                    }
                    if (null != dr[propertyName] && !dr[propertyName].Equals(DBNull.Value))
                    {
                        if (pi.PropertyType.IsGenericType)
                        {
                            if (pi.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                pi.SetValue(model, dr[propertyName], null);
                            }
                            else
                            {
                                pi.SetValue(model, Convert.ChangeType(dr[propertyName].ToString(), pi.PropertyType),
                                    null);
                            }
                        }
                        else
                        {
                            pi.SetValue(model, Convert.ChangeType(dr[propertyName].ToString(), pi.PropertyType), null);
                        }
                    }
                }
            }
            return model;
        }

        public static object InsertModel<T>(T model, string tableName)
        {
            var ssc = SimpleSqlCreater.Insert(tableName)
                .GetParamsFromClass(model);
            return ExecuteScalar(ssc.ToString());
        }

        public static int InsertDictionary(Dictionary<string, object> inserts, string tableName, string name = "con")
        {
            var ssc = SimpleSqlCreater.Insert(tableName)
                .GetParamsFromDictory(inserts);

            //使用dbCommand
            var db = GetDatabase(name);
            DbCommand insertCommand = db.GetSqlStringCommand(ssc.ToString());
            foreach (var insert in inserts)
            {
                if (insert.Value is byte[])
                {
                    db.AddInParameter(insertCommand, insert.Key, DbType.Binary, insert.Value);
                }
                else if (insert.Value is DateTime)
                {
                    db.AddInParameter(insertCommand, insert.Key, DbType.DateTime, insert.Value);
                }
                else
                {
                    db.AddInParameter(insertCommand, insert.Key, DbType.String, insert.Value);
                }
            }

            return db.ExecuteNonQuery(insertCommand);
        }

        public static object InsertModel<T>(T model)
        {
            return InsertModel(model, GetTableFromClass<T>());
        }

        public static int UpdateModel<T>(T model, string tableName)
        {
            var ssc = SimpleSqlCreater.Update(tableName);
            PropertyInfo[] pis = typeof(T).GetProperties();
            foreach (var propertyInfo in pis)
            {
                if (propertyInfo.Name == "Id")
                {
                    ssc.Eq("Id", propertyInfo.GetValue(model, null).ToString());
                    continue;
                }

                if (propertyInfo.CanWrite)
                {
                    string propertyName = propertyInfo.Name;
                    ssc.AddParam(propertyName, "'" + propertyInfo.GetValue(model, null).ToString().Replace("'", "''") + "'");
                }
            }
            return ExecuteNonQuery(ssc.ToString());
        }
        public static int UpdateModelByParams<T>(T model, string tableName, params string[] updateParams)
        {
            var ssc = SimpleSqlCreater.Update(tableName);
            PropertyInfo[] pis = typeof(T).GetProperties();
            foreach (var propertyInfo in pis)
            {
                if (propertyInfo.Name == "Id")
                {
                    ssc.Eq("Id", propertyInfo.GetValue(model, null).ToString());
                    continue;
                }

                if (propertyInfo.CanWrite && updateParams.Contains(propertyInfo.Name))
                {
                    string propertyName = propertyInfo.Name;
                    ssc.AddParam(propertyName, "'" + propertyInfo.GetValue(model, null).ToString().Replace("'", "''") + "'");
                }
            }
            return ExecuteNonQuery(ssc.ToString());
        }
        public static int UpdateModel<T>(T model)
        {
            return UpdateModel(model, GetTableFromClass<T>());
        }

        public static int UpdateModelByParams<T>(T model, params string[] updateParams)
        {
            return UpdateModelByParams(model, GetTableFromClass<T>(), updateParams);
        }

        public static int UpdateModel<T>(T model, string tableName, string keyFieldName, string keyValue, string separator = "'")
        {
            var ssc = SimpleSqlCreater.Update(tableName);
            PropertyInfo[] pis = typeof(T).GetProperties();
            foreach (var propertyInfo in pis)
            {
                if (propertyInfo.CanWrite)
                {
                    if (propertyInfo.Name == "Id") continue;

                    string propertyName = propertyInfo.Name;
                    ssc.AddParam(propertyName, "'" + propertyInfo.GetValue(model, null).ToString().Replace("'", "''") + "'");
                }
            }
            ssc.Eq(keyFieldName, keyValue, separator);
            return ExecuteNonQuery(ssc.ToString());
        }

        public static int Remove<T>(T model, string tableName)
        {
            var ssc = SimpleSqlCreater.Update(tableName);
            PropertyInfo[] pis = typeof(T).GetProperties();
            foreach (var propertyInfo in pis)
            {
                if (propertyInfo.CanWrite)
                {
                    ssc.Eq(propertyInfo.Name, propertyInfo.GetValue(model, null).ToString());
                }
            }
            return ExecuteNonQuery(ssc.ToString());
        }

        public static int Remove<T>(T model)
        {
            return Remove(model, GetTableFromClass<T>());
        }

        public static string GetTableFromClass<T>()
        {
            return typeof(T).Name + "T";
        }

        public static DataRow getModelToDataRow<T>(T model, DataTable dt)
        {
            Type tm = model.GetType();
            DataRow dr = dt.NewRow();
            foreach (DataColumn dc in dt.Columns)
            {
                string colName = dc.ColumnName;
                if (colName.ToUpper().Equals("Evenid".ToUpper()))
                    continue;
                PropertyInfo pi = tm.GetProperty(colName);
                object value = pi.GetValue(model, null);
                if (pi.PropertyType.IsGenericType)
                {
                    if (pi.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        if (value == null) value = DBNull.Value;
                    }
                }
                dr[colName] = value;
            }
            return dr;
        }
        public static DataRow getModelToDataRow<T>(T model, DataTable dt, string keyFieldName)
        {
            Type tm = model.GetType();
            DataRow dr = dt.Rows[0];
            foreach (DataColumn dc in dt.Columns)
            {
                string colName = dc.ColumnName;
                if (colName.ToUpper().Equals("Evenid".ToUpper()) || colName.ToUpper().Equals(keyFieldName.ToUpper()))
                    continue;
                PropertyInfo pi = tm.GetProperty(colName);
                object value = pi.GetValue(model, null);
                if (pi.PropertyType.IsGenericType)
                {
                    if (pi.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        if (value == null) value = DBNull.Value;
                    }
                }
                dr[colName] = value;
            }
            return dr;
        }
    }
}