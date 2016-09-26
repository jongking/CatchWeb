using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace JHelper.DB
{
    public class SimpleSqlCreater
    {
        private readonly string _table;
        private string _where;
        private readonly OperatorType _operatorType;
        private string _linker;
        private string _orderby;
        private int _limit;
        private Type _modelType;
        private Dictionary<string, string> _kyDictionary = new Dictionary<string, string>(); 

        private SimpleSqlCreater(string table, OperatorType operatorType)
        {
            _table = table;
            _operatorType = operatorType;
            _where = "";
            _limit = 0;
            _orderby = "";
            _linker = " AND ";
        }

        private SimpleSqlCreater(string table, OperatorType operatorType, Type type)
        {
            _table = table;
            _operatorType = operatorType;
            _where = "";
            _limit = 0;
            _orderby = "";
            _linker = " AND ";
            _modelType = type;
        }

        public static SimpleSqlCreater Select(string table)
        {
            return new SimpleSqlCreater(table, OperatorType.Select);
        }
        public static SimpleSqlCreater Insert(string table)
        {
            return new SimpleSqlCreater(table, OperatorType.Insert);
        }
        public static SimpleSqlCreater Update(string table)
        {
            return new SimpleSqlCreater(table, OperatorType.Update);
        }
        public static SimpleSqlCreater Delete(string table)
        {
            return new SimpleSqlCreater(table, OperatorType.Delete);
        }
        public static SimpleSqlCreater Where()
        {
            return new SimpleSqlCreater("", OperatorType.Where);
        }
        public static SimpleSqlCreater Select<T>()
        {
            var table = DbHelper.GetTableFromClass<T>();
            return new SimpleSqlCreater(table, OperatorType.Select);
        }
        public static SimpleSqlCreater Insert<T>()
        {
            var table = DbHelper.GetTableFromClass<T>();
            return new SimpleSqlCreater(table, OperatorType.Insert);
        }
        public static SimpleSqlCreater Update<T>()
        {
            var table = DbHelper.GetTableFromClass<T>();

            return new SimpleSqlCreater(table, OperatorType.Update);
        }
        public static SimpleSqlCreater Delete<T>()
        {
            var table = DbHelper.GetTableFromClass<T>();
            return new SimpleSqlCreater(table, OperatorType.Delete);
        }

        public static SimpleSqlCreater CreateTable<T>()
        {
            var table = DbHelper.GetTableFromClass<T>();
            return new SimpleSqlCreater(table, OperatorType.CreateTable, typeof(T));
        }

        public static SimpleSqlCreater DropTable(string table)
        {
            return new SimpleSqlCreater(table, OperatorType.DropTable);
        }

        public static SimpleSqlCreater DropTable<T>()
        {
            var table = DbHelper.GetTableFromClass<T>();
            return new SimpleSqlCreater(table, OperatorType.DropTable);
        }

        public SimpleSqlCreater Eq(string filedName, string param, string separator = "'")
        {
            _where += _linker + string.Format(" {0} = {2}{1}{2} ", filedName, param.Replace("'", "''"), separator);
            return this;
        }
        public SimpleSqlCreater BigEq(string filedName, string param, string separator = "'")
        {
            _where += _linker + string.Format(" {0} >= {2}{1}{2} ", filedName, param.Replace("'", "''"), separator);
            return this;
        }
        public SimpleSqlCreater SmlEq(string filedName, string param, string separator = "'")
        {
            _where += _linker + string.Format(" {0} <= {2}{1}{2} ", filedName, param.Replace("'", "''"), separator);
            return this;
        }
        public SimpleSqlCreater Big(string filedName, string param, string separator = "'")
        {
            _where += _linker + string.Format(" {0} > {2}{1}{2} ", filedName, param.Replace("'", "''"), separator);
            return this;
        }
        public SimpleSqlCreater Sml(string filedName, string param, string separator = "'")
        {
            _where += _linker + string.Format(" {0} < {2}{1}{2} ", filedName, param.Replace("'", "''"), separator);
            return this;
        }
        public SimpleSqlCreater Like(string filedName, string param, string separator = "'")
        {
            _where += _linker + string.Format(" {0} LIKE {2}%{1}%{2} ", filedName, param.Replace("'", "''"), separator);
            return this;
        }
        public SimpleSqlCreater NotLike(string filedName, string param, string separator = "'")
        {
            _where += _linker + string.Format(" {0} NOT LIKE {2}%{1}%{2} ", filedName, param.Replace("'", "''"), separator);
            return this;
        }
        public SimpleSqlCreater LLike(string filedName, string param, string separator = "'")
        {
            _where += _linker + string.Format(" {0} LIKE {2}%{1}{2} ", filedName, param.Replace("'", "''"), separator);
            return this;
        }
        public SimpleSqlCreater RLike(string filedName, string param, string separator = "'")
        {
            _where += _linker + string.Format(" {0} LIKE {2}{1}%{2} ", filedName, param.Replace("'", "''"), separator);
            return this;
        }
        public SimpleSqlCreater In(string filedName, string param)
        {
            _where += _linker + string.Format(" {0} IN ({1}) ", filedName, param);
            return this;
        }
        public SimpleSqlCreater NotIn(string filedName, string param)
        {
            _where += _linker + string.Format(" {0} NOT IN ({1}) ", filedName, param);
            return this;
        }
        public SimpleSqlCreater And()
        {
            _linker = " AND ";
            return this;
        }
        public SimpleSqlCreater Or()
        {
            _linker = " OR  ";
            return this;
        }

        public SimpleSqlCreater Limit(int num)
        {
            _limit = num;
            return this;
        }

        public SimpleSqlCreater OrderBy(string fildName, OrderByType orderBy)
        {
            _orderby = string.Format(orderBy == OrderByType.Asc ? " {0} ASC " : " {0} DESC ", fildName);
            return this;
        }

        public SimpleSqlCreater AddParam(string filedName, string param)
        {
            _kyDictionary.Add(filedName, param);
            return this;
        }

        public SimpleSqlCreater SetParam(string filedName, string param)
        {
            if (_kyDictionary.ContainsKey(filedName))
            {
                _kyDictionary[filedName] = param;
            }
            else
            {
                AddParam(filedName, param);
            }
            return this;
        }

        public SimpleSqlCreater Combine(SimpleSqlCreater ssc)
        {
            _where += _linker + ssc;
            return this;
        }

        public SimpleSqlCreater GetParamsFromClass<T>(T model)
        {
            PropertyInfo[] pis = typeof(T).GetProperties();
            foreach (var propertyInfo in pis)
            {
                if (propertyInfo.CanWrite)
                {
                    if (propertyInfo.Name == "Id") continue;

                    string propertyName = propertyInfo.Name;
                    this.AddParam(propertyName, "'" + propertyInfo.GetValue(model, null).ToString().Replace("'", "''") + "'");
                }
            }
            return this;
        }

        public SimpleSqlCreater GetParamsFromDictory(Dictionary<string, object> dicts)
        {
            foreach (var dict in dicts)
            {
                if (dict.Key == "Id") continue;

                string propertyName = dict.Key;
                this.AddParam(propertyName, "@" + dict.Key + "");
            }

            return this;
        }

        public override string ToString()
        {
            if (_operatorType == OperatorType.Select)
            {
                string limit = "";
                string orderby = "";
                if (_limit > 0)
                {
                    limit = string.Format(" TOP {0}", _limit);
                }
                if (_orderby != "")
                {
                    orderby = string.Format(" ORDER BY {0} ", _orderby);
                }
                return string.Format("SELECT {2} * FROM {0} WHERE 1=1 {1} {3}", _table, _where, limit, orderby);
            }
            if (_operatorType == OperatorType.Insert)
            {
                string fileNames = "";
                string pars = "";
                foreach (var v in _kyDictionary)
                {
                    fileNames += v.Key + ",";
                    pars += v.Value + ",";
                }
                if (fileNames.Length > 0) fileNames = fileNames.Substring(0, fileNames.Length - 1);
                if (pars.Length > 0) pars = pars.Substring(0, pars.Length - 1);

                return string.Format("INSERT INTO {0} ({1}) VALUES ({2})", _table, fileNames, pars);
            }
            if (_operatorType == OperatorType.Update)
            {
                string fileNames = "";
                foreach (var v in _kyDictionary)
                {
                    fileNames += v.Key + " = " + v.Value + " ,";
                }
                if (fileNames.Length > 0) fileNames = fileNames.Substring(0, fileNames.Length - 1);

                return string.Format("UPDATE {0} SET {2} WHERE 1=1 {1}", _table, _where, fileNames);
            }
            if (_operatorType == OperatorType.Delete)
            {
                return string.Format("DELETE FROM {0} WHERE 1=1 {1}", _table, _where);
            }
            if (_operatorType == OperatorType.Where)
            {
                return "(" + _where.Substring(5) + ")";
            }
            if (_operatorType == OperatorType.CreateTable)
            {
                var tempstr = "";
                PropertyInfo[] pis = _modelType.GetProperties();
                bool haveId = false;

                foreach (var propertyInfo in pis)
                {
                    if (propertyInfo.CanWrite)
                    {
                        string propertyName = propertyInfo.Name;
                        if (propertyName.ToLower() == "id")
                        {
                            haveId = true;
                            tempstr += string.Format("{0} [bigint] IDENTITY(1,1) NOT NULL,", propertyName);
                            continue;
                        }
                        switch (propertyInfo.PropertyType.Name.ToLower())
                        {
                            case "string":
                                tempstr += string.Format("{0} [nvarchar](100),", propertyName);
                                break;
                            case "datetime":
                                tempstr += string.Format("{0} [datetime],", propertyName);
                                break;
                            case "int32":
                                tempstr += string.Format("{0} [bigint],", propertyName);
                                break;
                            case "int64":
                                tempstr += string.Format("{0} [bigint],", propertyName);
                                break;
                            case "double":
                                tempstr += string.Format("{0} [float],", propertyName);
                                break;
                            case "bool":
                                tempstr += string.Format("{0} [bit],", propertyName);
                                break;
                        }
                    }
                }

                if (haveId)
                {
                    tempstr += " PRIMARY KEY CLUSTERED ( [Id] ASC )WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]";
                }
                return string.Format("CREATE TABLE {0} ({1})", _table, tempstr);
            }
            if (_operatorType == OperatorType.DropTable)
            {
                return string.Format("IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'{0}') AND type in (N'U')) DROP TABLE {0}", _table);
            }
            return "ERROR";
        }

        public enum OperatorType
        {
            Select,
            Insert,
            Update,
            Delete,
            Where,
            CreateTable,
            DropTable
        }

        public enum OrderByType
        {
            Desc,
            Asc
        }
    }
}
