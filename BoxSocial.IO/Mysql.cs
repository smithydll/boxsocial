/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Lachlan Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 3 as
 * published by the Free Software Foundation.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Text;
using System.Web;
using MySql.Data;

namespace BoxSocial.IO
{
    public class Mysql : Database
    {
        public const int TRANSACTION_ROLLBACK = -1;
        public const int TRANSACTION_ROLLBACK_FAIL = -2;

        private MySql.Data.MySqlClient.MySqlConnection sqlConnection;
        private MySql.Data.MySqlClient.MySqlTransaction sqlTransaction;
        private MySql.Data.MySqlClient.MySqlCommand sqlCommand;
        private bool inTransaction = false;

        public string QueryList = "";

        public Mysql(string username, string database, string host)
        {
            queryCount = 0;
            connectionString = "username=" + username + ";Host=" + host + ";Database=" + database + ";";
            Connect();
        }

        public Mysql(string username, string password, string database, string host)
        {
            queryCount = 0;
            if (password.Length > 0)
            {
                connectionString = "username=" + username + ";Host=" + host + ";Database=" + database + ";Password=" + password + ";";
            }
            else
            {
                connectionString = "username=" + username + ";Host=" + host + ";Database=" + database + ";";
            }
            Connect();
        }

        public Mysql(string connectionString)
        {
            this.connectionString = connectionString;
            Connect();
        }

        public Mysql()
        {
            connectionString = "";
            Connect();
        }

        private new void Connect()
        {
            sqlConnection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
            sqlConnection.Open();
        }

        private DataTable SelectQuery(string sqlquery)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            queryCount++;
            sqlquery = sqlquery.Replace("\\", "\\\\");
            QueryList += sqlquery + "\n";

            DataTable resultTable;
            try
            {
                QueryList += sqlConnection.State.ToString() + "\n\n";
                DataSet resultSet = new DataSet();
                MySql.Data.MySqlClient.MySqlDataAdapter dataAdapter = new MySql.Data.MySqlClient.MySqlDataAdapter();
                dataAdapter.SelectCommand = new MySql.Data.MySqlClient.MySqlCommand(sqlquery, sqlConnection);
                dataAdapter.Fill(resultSet);

                resultTable = resultSet.Tables[0];

                timer.Stop();
                queryTime += timer.ElapsedTicks;

                return resultTable;
            }
            catch (System.Exception ex)
            {
                throw new System.Exception(ex.ToString());
                //return new DataTable();
            }
        }

        private DataTable SelectQuery(SelectQuery query)
        {
            return SelectQuery(query.ToString());
        }

        private long UpdateQuery(Query query)
        {
            return UpdateQuery(query.ToString());
        }

        public string Status()
        {
            return sqlConnection.State.ToString() + ((inTransaction) ? " transaction" : "");
        }

        /// <summary>
        /// On an INSERT INTO query returns the ID of the inserted query. On an update it returns number of rows Affected. On Error returns -1.
        /// </summary>
        /// <param name="sqlquery"></param>
        /// <returns></returns>
        public override long UpdateQuery(string sqlquery)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            int rowsAffected = 0;
            queryCount++;
            QueryList += sqlquery + "\n";

            if (sqlCommand == null)
            {
                CommitTransaction();
                sqlCommand = sqlConnection.CreateCommand();
            }

            try
            {
                sqlCommand.CommandText = sqlquery;
                QueryList += sqlConnection.State.ToString() + "\n\n";
                rowsAffected = sqlCommand.ExecuteNonQuery();
            }
            catch (System.Exception ex)
            {
                try
                {
                    if (inTransaction)
                    {
                        sqlTransaction.Rollback();
                    }
                    else
                    {
                        throw new System.Exception(sqlquery + "\n\n" + ex.ToString());
                    }
                }
                catch (MySql.Data.MySqlClient.MySqlException)
                {
                    return TRANSACTION_ROLLBACK_FAIL; // failed rollback
                }
                finally
                {
                }
                return TRANSACTION_ROLLBACK; // rollback
            }

            timer.Stop();
            queryTime += timer.ElapsedTicks;

            if (sqlquery.StartsWith("INSERT INTO"))
            {
                return sqlCommand.LastInsertedId;
            }
            else
            {
                return rowsAffected;
            }
        }

        public void BeginTransaction()
        {
            if (!inTransaction)
            {
                if (sqlCommand == null)
                {
                    sqlCommand = sqlConnection.CreateCommand();
                }

                sqlTransaction = sqlConnection.BeginTransaction(); // Snapshot
                sqlCommand.Transaction = sqlTransaction;
                inTransaction = true;

                QueryList += "BEGIN TRANSACTION\n\n";
            }
        }

        public bool CommitTransaction()
        {
            if (inTransaction)
            {
                QueryList += "COMMIT TRANSACTION\n\n";
                inTransaction = false;
                try
                {
                    sqlTransaction.Commit();
                    return true;
                }
                catch
                {
                    try
                    {
                        RollBackTransaction();
                    }
                    catch (MySql.Data.MySqlClient.MySqlException)
                    {
                        // rollback failed
                    }
                    catch (InvalidOperationException)
                    {
                        // rollback failed
                    }
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public void RollBackTransaction()
        {
            sqlTransaction.Rollback();
        }

        public void CloseConnection()
        {
            CommitTransaction();
            if (sqlTransaction != null)
            {
                try
                {
                    sqlTransaction.Dispose();
                }
                catch (InvalidOperationException)
                {
                    // ignore, mono bug?
                }
            }
            if (sqlCommand != null)
            {
                sqlCommand.Dispose();
            }
            sqlConnection.Close();
            sqlConnection.Dispose();
        }

        public static string Escape(string input)
        {
            return input.Replace("'", "''").Replace("\\", "\\\\").Replace("’", "\\’");
        }

        ~Mysql()
        {
            CloseConnection();
        }

        public override DataTable Query(SelectQuery query)
        {
            return SelectQuery(query.ToString());
        }

        public override long Query(InsertQuery query)
        {
            return UpdateQuery(query.ToString());
        }

        public override long Query(UpdateQuery query)
        {
            return UpdateQuery(query.ToString());
        }

        public override long Query(DeleteQuery query)
        {
            return UpdateQuery(query.ToString());
        }

        public override DataTable Query(string query)
        {
            return SelectQuery(query);
        }

        public override bool TableExists(string tableName)
        {
            DataTable tableTable = SelectQuery("SHOW TABLES");

            foreach (DataRow dr in tableTable.Rows)
            {
                if ((string)dr[0] == tableName)
                {
                    return true;
                }
            }

            return false;
        }

        public override Dictionary<string, DataFieldInfo> GetColumns(string tableName)
        {
            Dictionary<string, DataFieldInfo> fields = new Dictionary<string, DataFieldInfo>();

            DataTable fieldTable = SelectQuery(string.Format("SHOW COLUMNS FROM `{0}`",
                Mysql.Escape(tableName)));

            foreach (DataRow dr in fieldTable.Rows)
            {
                DataFieldInfo dfi = MysqlToType((string)dr["Field"], (string)dr["Type"]);

                if (((string)dr["Key"]).ToUpper() == "PRI")
                {
                    dfi.IsUnique = dfi.IsPrimaryKey = true;
                }
                if (((string)dr["Key"]).ToUpper() == "UNI")
                {
                    dfi.IsUnique = true;
                }

                fields.Add((string)dr["Field"], dfi);
            }

            return fields;
        }

        public override Dictionary<string, List<DataField>> GetIndexes(string tableName)
        {
            Dictionary<string, List<DataField>> indexes = new Dictionary<string, List<DataField>>();

            DataTable fieldTable = SelectQuery(string.Format("SHOW INDEXES FROM `{0}`",
                Mysql.Escape(tableName)));

            foreach (DataRow dr in fieldTable.Rows)
            {
                DataField df = new DataField((string)dr["Table"], (string)dr["Column_name"]);
                string key = (string)dr["Key_name"];

                if (indexes.ContainsKey(key) == false)
                {
                    indexes.Add(key, new List<DataField>());
                }

                indexes[key].Add(df);
            }

            return indexes;
        }

        private DataFieldInfo MysqlToType(string name, string type)
        {
            type = type.ToLower();

            bool unsigned = type.EndsWith("unsigned");
            long length = 0;
            int indexOpenBracket = type.IndexOf("(");
            int indexCloseBracket = type.IndexOf(")");

            if (!type.ToLower().Contains("enum("))
            {
                if (indexOpenBracket >= 0 && indexCloseBracket > indexOpenBracket)
                {
                    length = long.Parse(type.Substring(indexOpenBracket + 1, indexCloseBracket - indexOpenBracket - 1));
                }
            }

            switch (type.Split(new char[] { '(' })[0])
            {
                case "bigint":
                    if (unsigned)
                    {
                        return new DataFieldInfo(name, typeof(ulong), length);
                    }
                    else
                    {
                        return new DataFieldInfo(name, typeof(long), length);
                    }
                case "int":
                    if (unsigned)
                    {
                        return new DataFieldInfo(name, typeof(uint), length);
                    }
                    else
                    {
                        return new DataFieldInfo(name, typeof(int), length);
                    }
                case "smallint":
                    if (unsigned)
                    {
                        return new DataFieldInfo(name, typeof(ushort), length);
                    }
                    else
                    {
                        return new DataFieldInfo(name, typeof(short), length);
                    }
                case "tinyint":
                case "tiny":
                    if (length == 1)
                    {
                        return new DataFieldInfo(name, typeof(bool), 0);
                    }
                    else
                    {
                        if (unsigned)
                        {
                            return new DataFieldInfo(name, typeof(byte), length);
                        }
                        else
                        {
                            return new DataFieldInfo(name, typeof(sbyte), length);
                        }
                    }
                case "float":
                    return new DataFieldInfo(name, typeof(float), 0);
                case "varchar":
                    return new DataFieldInfo(name, typeof(string), length);
                case "text":
                    return new DataFieldInfo(name, typeof(string), 65535L);
                case "mediumtext":
                    return new DataFieldInfo(name, typeof(string), 16777215L);
                case "longtext":
                    return new DataFieldInfo(name, typeof(string), 4294967295L);
                case "enum":
                    return new DataFieldInfo(name, typeof(string), 255L);
                case "char":
                    return new DataFieldInfo(name, typeof(char[]), length);
                default:
                    return new DataFieldInfo(name, typeof(Object), length);
            }
        }

        private string TypeToMysql(DataFieldInfo type)
        {
            if (type.Type == typeof(ulong))
            {
                return "bigint(20) unsigned";
            }
            else if (type.Type == typeof(long))
            {
                return "bigint(20)";
            }
            else if (type.Type == typeof(uint))
            {
                return "int(11) unsigned";
            }
            else if (type.Type == typeof(int))
            {
                return "int(11)";
            }
            else if (type.Type == typeof(ushort))
            {
                return "smallint(5) unsigned";
            }
            else if (type.Type == typeof(short))
            {
                return "smallint(5)";
            }
            else if (type.Type == typeof(byte))
            {
                return "tinyint(3) unsigned";
            }
            else if (type.Type == typeof(sbyte))
            {
                return "tinyint(3)";
            }
            else if (type.Type == typeof(bool))
            {
                return "tinyint(1) unsigned";
            }
            else if (type.Type == typeof(float))
            {
                return "float";
            }
            else if (type.Type == typeof(string))
            {
                if (type.Length < 256)
                {
                    return string.Format("varchar({0})",
                        type.Length);
                }
                else if (type.Length < 65536)
                {
                    return "text";
                }
                else if (type.Length < 16777216)
                {
                    return "mediumtext";
                }
                else
                {
                    return "longtext";
                }
            }
            else if (type.Type == typeof(char[]))
            {
                return string.Format("char({0})",
                        type.Length);
            }
            else
            {
                return "varchar(255)";
            }
        }

        public override void AddColumn(string tableName, DataFieldInfo field)
        {
            string type = TypeToMysql(field);
            string defaultValue = "";
            string notNull = "";

            if (type.ToLower() == "text" || type.ToLower() == "mediumtext" || type.ToLower() == "longtext")
            {
                notNull = " NOT NULL";
            }

            if (type.ToLower().Contains("int("))
            {
                if (field.IsPrimaryKey)
                {
                    notNull = " NOT NULL";
                    defaultValue = " DEFAULT NULL AUTO_INCREMENT";
                }
                else
                {
                    defaultValue = " DEFAULT 0";
                }
            }
            if (type.ToLower() == "float")
            {
                defaultValue = " DEFAULT 0";
            }

            string query = string.Format(@"ALTER TABLE `{0}` ADD COLUMN `{1}` {2}{3}{4};",
                Mysql.Escape(tableName), Mysql.Escape(field.Name), type, notNull, defaultValue);

            UpdateQuery(query);
        }

        public override void AddColumns(string tableName, List<DataFieldInfo> fields)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(string.Format(@"ALTER TABLE `{0}`",
                Mysql.Escape(tableName)));

            bool first = true;

            foreach (DataFieldInfo field in fields)
            {
                string type = TypeToMysql(field);
                string defaultValue = "";
                string notNull = "";

                if (type.ToLower() == "text" || type.ToLower() == "mediumtext" || type.ToLower() == "longtext")
                {
                    notNull = " NOT NULL";
                }

                if (type.ToLower().Contains("int("))
                {
                    if (field.IsPrimaryKey)
                    {
                        notNull = " NOT NULL";
                        defaultValue = " DEFAULT NULL AUTO_INCREMENT";
                    }
                    else
                    {
                        defaultValue = " DEFAULT 0";
                    }
                }
                if (type.ToLower() == "float")
                {
                    defaultValue = " DEFAULT 0";
                }

                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(",");
                }

                sb.Append(string.Format(@" ADD COLUMN `{0}` {1}{2}{3}",
                    Mysql.Escape(field.Name), type, notNull, defaultValue));
            }

            sb.Append(";");

            UpdateQuery(sb.ToString());
        }

        public override void ChangeColumn(string tableName, DataFieldInfo field)
        {
            string type = TypeToMysql(field);
            string defaultValue = "";
            string notNull = "";

            if (field.Type == typeof(string))
            {
                if (field.Length == 0)
                {
                    throw new Exception("String Length must not be zero thrown on " + field.Name);
                }
            }

            if (type.ToLower() == "text" || type.ToLower() == "mediumtext" || type.ToLower() == "longtext")
            {
                notNull = " NOT NULL";
            }

            if (type.ToLower().Contains("int("))
            {
                if (field.IsPrimaryKey)
                {
                    notNull = " NOT NULL";
                    defaultValue = " DEFAULT NULL AUTO_INCREMENT";
                }
                else
                {
                    defaultValue = " DEFAULT 0";
                }
            }

            string query = string.Format(@"ALTER TABLE `{0}` MODIFY COLUMN `{1}` {2}{3}{4};",
                Mysql.Escape(tableName), Mysql.Escape(field.Name), type, notNull, defaultValue);

            try
            {
                UpdateQuery(query);
            }
            catch
            {
                HttpContext.Current.Response.Write(query.ToString());
                HttpContext.Current.Response.End();
            }
        }

        public override void DeleteColumn(string tableName, string fieldName)
        {
            string query = string.Format(@"ALTER TABLE `{0}` DROP COLUMN `{1}`;",
                Mysql.Escape(tableName), Mysql.Escape(fieldName));
        }

        public override void CreateTable(string tableName, List<DataFieldInfo> fields)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder indexes = new StringBuilder();
            DataFieldInfo primaryKey = null;

            sb.Append(string.Format("CREATE TABLE IF NOT EXISTS `{0}` (",
                Mysql.Escape(tableName)));

            bool first = true;
            foreach (DataFieldInfo field in fields)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }

                string type = TypeToMysql(field);
                string notNull = "";
                string key = "";
                string defaultValue = "";

                if (type == "text" || type == "mediumtext" || type == "longtext")
                {
                    notNull = " NOT NULL";
                }

                if (type.ToLower().Contains("int("))
                {
                    if (field.IsPrimaryKey)
                    {
                        notNull = " NOT NULL";
                        defaultValue = " DEFAULT NULL";
                    }
                    else
                    {
                        defaultValue = " DEFAULT 0";
                    }
                }
                if (type.ToLower() == "float")
                {
                    defaultValue = " DEFAULT 0";
                }

                if (field.IsUnique)
                {
                    notNull = " NOT NULL";
                    if (field.IsPrimaryKey)
                    {
                        primaryKey = field;
                        key = " AUTO_INCREMENT";
                        indexes.Append(string.Format(", PRIMARY_KEY(`{0}`)",
                            Mysql.Escape(field.Name)));
                    }
                    else if (field.Key != null)
                    {
                        key = " UNIQUE KEY";
                        indexes.Append(string.Format(", INDEX(`{0}`)",
                            Mysql.Escape(field.Name)));
                    }
                    else
                    {
                        //key = " NOT NULL UNIQUE KEY";
                    }
                }

                sb.Append(string.Format("`{0}` {1}{2}{3}{4}",
                    Mysql.Escape(field.Name), type, notNull, defaultValue, key));
            }

            if (primaryKey != null)
            {
                sb.Append(string.Format(", PRIMARY KEY (`{0}`)",
                    Mysql.Escape(primaryKey.Name)));
            }

            List<string> keys = UniqueKey.GetKeys(fields);

            foreach (string key in keys)
            {
                List<DataFieldInfo> keyFields = UniqueKey.GetFields(key, fields);

                sb.Append(string.Format(", UNIQUE INDEX `{0}` (",
                    Mysql.Escape(key)));

                bool firstKey = true;
                foreach (DataFieldInfo keyField in keyFields)
                {
                    if (!firstKey)
                    {
                        sb.Append(", ");
                    }
                    else
                    {
                        firstKey = false;
                    }

                    sb.Append(string.Format("`{0}`",
                        Mysql.Escape(keyField.Name)));
                }

                sb.Append(")");
            }

            sb.Append(") ENGINE=InnoDB DEFAULT CHARSET=utf8");

            UpdateQuery(sb.ToString());
        }

        public override void UpdateTableKeys(string tableName, List<DataFieldInfo> fields)
        {
            Dictionary<string, List<DataField>> indexes = GetIndexes(tableName);

            List<string> keys = UniqueKey.GetKeys(fields);

            foreach (string key in indexes.Keys)
            {
                bool removeKey = !keys.Contains(key);
                bool fieldKey = false;

                if (indexes[key].Count == 1)
                {
                    foreach (DataFieldInfo field in fields)
                    {
                        if (field.IsUnique && field.Key == null && indexes[key][0].Name == field.Name)
                        {
                            fieldKey = true;
                            break;
                        }
                    }
                }

                if (removeKey && (!fieldKey))
                {
                    // delete the key
                    string sql = string.Format("ALTER TABLE `{0}` DROP INDEX `{1}`",
                        tableName, key);

                    UpdateQuery(sql);
                }
            }

            foreach (string key in keys)
            {
                UniqueKey thisKey = new UniqueKey(key);
                List<DataFieldInfo> keyFields = UniqueKey.GetFields(key, fields);
                bool newKey = !indexes.ContainsKey(key);

                if (!newKey)
                {
                    bool keyFieldsChanged = false;

                    foreach (DataFieldInfo keyField in keyFields)
                    {
                        if (!indexes[key].Contains(new DataField(tableName, keyField.Name)))
                        {
                            keyFieldsChanged = true;
                            newKey = true;
                        }
                    }

                    foreach (DataField field in indexes[key])
                    {
                        bool flag = false;

                        foreach (DataFieldInfo keyField in keyFields)
                        {
                            if (keyField.Name == field.Name)
                            {
                                flag = true;
                                break;
                            }
                        }

                        if (!flag)
                        {
                            keyFieldsChanged = true;
                            newKey = true;
                        }
                    }

                    if (keyFieldsChanged)
                    {
                        // delete the key
                        string sql = string.Format("ALTER TABLE `{0}` DROP INDEX `{1}`",
                            tableName, key);

                        UpdateQuery(sql);
                    }
                }

                if (newKey)
                {
                    // create the key
                    bool first = true;
                    string fieldList = "";
                    
                    foreach (DataFieldInfo keyField in keyFields)
                    {
                        if (!first)
                        {
                            fieldList += ", ";
                        }
                        first = false;

                        fieldList += "`" + keyField.Name + "`";
                    }

                    string sql = string.Format("ALTER TABLE `{0}` ADD UNIQUE INDEX `{1}` ({2});",
                        tableName, key, fieldList);

                    UpdateQuery(sql);
                }
            }
        }
    }
}