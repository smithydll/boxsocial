/*
 * Box Social™
 * http://boxsocial.net/
 * Copyright © 2007, David Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 2 of
 * the License, or (at your option) any later version.
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

        public Stack<string> QueryList = new Stack<string>(64);
        public StringBuilder ErrorList = new StringBuilder(8);

        public Mysql(string username, string database, string host)
        {
            queryCount = 0;
            //connectionString = "username=" + username + ";Host=" + host + ";Database=" + database + ";";
            connectionString = "Server=" + host + ";Database=" + database + ";Uid=" + username + ";";
            Connect();
        }

        public Mysql(string username, string password, string database, string host)
        {
            queryCount = 0;
            if (password.Length > 0)
            {
                //connectionString = "username=" + username + ";Host=" + host + ";Database=" + database + ";Password=" + password + ";";
                connectionString = "Server=" + host + ";Database=" + database + ";Uid=" + username + ";Pwd=" + password + ";";
            }
            else
            {
                //connectionString = "username=" + username + ";Host=" + host + ";Database=" + database + ";";
                connectionString = "Server=" + host + ";Database=" + database + ";Uid=" + username + ";";
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
            connectionString = String.Empty;
            Connect();
        }

        public void ResetQueryLog()
        {
            QueryList = new Stack<string>(64);
            ErrorList = new StringBuilder(8);
        }

        private void PushQuery(string query, double time)
        {
//#if DEBUG
            /*string[] stacktrace = Environment.StackTrace.Split(new char[] { '\n' });
            if (stacktrace.Length > 7)
            {
                QueryList.Push(QueryList.Count + ": query executed in " + time.ToString() + " seconds\r\n" + query + "\r\n\r\n" + stacktrace[4].TrimEnd(new char[] { '\r' }) + "\r\n" + stacktrace[5].TrimEnd(new char[] { '\r' }) + "\r\n" + stacktrace[6].TrimEnd(new char[] { '\r' }) + "\r\n" + stacktrace[7].TrimEnd(new char[] { '\r' }) + "\r\n====================\r\n");
            }
            else*/
            {
                QueryList.Push(QueryList.Count + ": query executed in " + time.ToString() + " seconds\r\n" + query + "\r\n====================\r\n");
            }
            //QueryList.Push(query);

            if (QueryList.Count > 100)
            {
                QueryList.Pop();
            }
//#endif
        }

        public string QueryListToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (string s in QueryList)
            {
                sb.AppendLine(s);
            }

            return sb.ToString();
        }

        private new void Connect()
        {
            if (sqlConnection == null || sqlConnection.State == ConnectionState.Closed || sqlConnection.State == ConnectionState.Broken)
            {
                sqlConnection = new MySql.Data.MySqlClient.MySqlConnection(connectionString);
                sqlConnection.Open();
            }
        }

        private DataTable SelectQuery(string sqlquery)
        {
            //MySql.Data.MySqlClient.MySqlConnection.ClearPool(sqlConnection);
            Stopwatch timer = new Stopwatch();
            timer.Start();

            queryCount++;
            sqlquery = sqlquery.Replace("\\", "\\\\");

            Connect();

            DataTable resultTable = new DataTable();
            try
            {
                PushQuery(sqlConnection.State.ToString(), 0.0);

                //DataSet resultSet = new DataSet();
                MySql.Data.MySqlClient.MySqlDataAdapter dataAdapter = new MySql.Data.MySqlClient.MySqlDataAdapter();
                dataAdapter.SelectCommand = new MySql.Data.MySqlClient.MySqlCommand(sqlquery, sqlConnection);
                long qt = timer.ElapsedTicks;
                PushQuery("Query", qt / 10000000.0);
                //dataAdapter.Fill(resultSet);
                System.Data.Common.DbDataReader resultReader = dataAdapter.SelectCommand.ExecuteReader();
                resultTable.Load(resultReader);
                resultReader.Close();
                resultReader.Dispose();
                PushQuery("Fill", (timer.ElapsedTicks - qt) / 10000000.0);
                dataAdapter.SelectCommand.Dispose();
                dataAdapter.Dispose();

                //resultTable = resultSet.Tables[0];

                timer.Stop();
                queryTime += qt;
                PushQuery(sqlquery, timer.ElapsedTicks / 10000000.0);

                return resultTable;
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                PushQuery(sqlquery, 0.0);
                throw new System.Exception(ex.ToString());
            }
            catch (System.Exception ex)
            {
                PushQuery(sqlquery, 0.0);
                throw new System.Exception(ex.ToString());
            }
        }

        private System.Data.Common.DbDataReader SelectReaderQuery(string sqlquery)
        {
            //MySql.Data.MySqlClient.MySqlConnection.ClearPool(sqlConnection);
            Stopwatch timer = new Stopwatch();
            timer.Start();

            queryCount++;
            sqlquery = sqlquery.Replace("\\", "\\\\");

            Connect();

            MySql.Data.MySqlClient.MySqlDataAdapter dataAdapter = new MySql.Data.MySqlClient.MySqlDataAdapter();
            dataAdapter.SelectCommand = new MySql.Data.MySqlClient.MySqlCommand(sqlquery, sqlConnection);
            System.Data.Common.DbDataReader resultReader = dataAdapter.SelectCommand.ExecuteReader();
            dataAdapter.SelectCommand.Dispose();
            dataAdapter.Dispose();

            timer.Stop();
            queryTime += timer.ElapsedTicks;
            PushQuery(sqlquery, timer.ElapsedTicks / 10000000.0);

            return resultReader;
        }

        private DataTable SelectQuery(SelectQuery query)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            string q = query.ToString();

            timer.Stop();
            queryTime += timer.ElapsedTicks;
#if DEBUG
            if (HttpContext.Current != null)
            {
                //HttpContext.Current.Response.Write(string.Format("<!-- Query rendered in {0} seconds ({1}) -->\r\n", timer.ElapsedTicks / 10000000.0, query.Tables[0]));
            }
#endif

            return SelectQuery(q);
        }

        private long UpdateQuery(Query query)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            string q = query.ToString();

            timer.Stop();
            queryTime += timer.ElapsedTicks;
#if DEBUG
            if (HttpContext.Current != null)
            {
                //HttpContext.Current.Response.Write(string.Format("<!-- Query rendered in {0} seconds ({1}) -->\r\n", timer.ElapsedTicks / 10000000.0, query.Tables[0]));
            }
#endif

            return UpdateQuery(q);
        }

        public string Status()
        {
#if DEBUG
            return sqlConnection.State.ToString() + ((inTransaction) ? " transaction" : string.Empty);
#else
            return string.Empty;
#endif
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

            Connect();

            if (sqlCommand == null)
            {
                CommitTransaction();
                sqlCommand = sqlConnection.CreateCommand();
            }

            try
            {
                sqlCommand.CommandText = sqlquery;
                PushQuery(sqlConnection.State.ToString(), 0.0);
                rowsAffected = sqlCommand.ExecuteNonQuery();
            }
            catch (System.Exception ex)
            {
                ErrorList.AppendLine("Error in SQL statement:\n" + sqlquery + "\n\n" + ex.ToString() + "\n--------------------------------------------------------------------------------\n");

                if (inTransaction)
                {
                    try
                    {
                        RollBackTransaction();
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
                else
                {
                    throw new System.Exception(sqlquery + "\n\n" + QueryListToString() + "\n\n" + ex.ToString());
                }
            }

            timer.Stop();
            queryTime += timer.ElapsedTicks;
            PushQuery(sqlquery, timer.ElapsedTicks / 10000000.0);

            if (sqlquery.StartsWith("INSERT INTO", StringComparison.Ordinal))
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

                PushQuery("BEGIN TRANSACTION", 0.0);
            }
        }

        public bool CommitTransaction()
        {
            if (inTransaction)
            {
                PushQuery("COMMIT TRANSACTION", 0.0);
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
            return input.Replace("'", "''").Replace("\\", "\\\\").Replace("", "\\");
        }

        ~Mysql()
        {
            CloseConnection();
        }

        public override System.Data.Common.DbDataReader ReaderQuery(SelectQuery query)
        {
#if DEBUG
            Stopwatch timer = new Stopwatch();
            timer.Start();
#endif

            string q = query.ToString();

#if DEBUG
            timer.Stop();
            queryTime += timer.ElapsedTicks;
#endif

            return SelectReaderQuery(q);
        }

        public override DataTable Query(SelectQuery query)
        {
            DataTable selectDataTable = SelectQuery(query);

            return selectDataTable;
        }

        public override long Query(InsertQuery query)
        {
            return UpdateQuery(query);
        }

        public override long Query(UpdateQuery query)
        {
            return UpdateQuery(query);
        }

        public override long Query(DeleteQuery query)
        {
            return UpdateQuery(query);
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
            Dictionary<string, DataFieldInfo> fields = new Dictionary<string, DataFieldInfo>(32, StringComparer.Ordinal);

            DataTable fieldTable = SelectQuery(string.Format("SHOW COLUMNS FROM `{0}`",
                Mysql.Escape(tableName)));

            foreach (DataRow dr in fieldTable.Rows)
            {
                DataFieldInfo dfi = MysqlToType((string)dr["Field"], (string)dr["Type"]);

                if (((string)dr["Key"]).ToUpper() == "PRI")
                {
					dfi.Key = DataFieldKeys.Primary;
                }
                if (((string)dr["Key"]).ToUpper() == "UNI")
                {
					dfi.Key = DataFieldKeys.Unique;
                }

                fields.Add((string)dr["Field"], dfi);
            }

            return fields;
        }
		
		private List<DataFieldInfo> GetFields(string tableName)
		{
			Dictionary<string, DataFieldInfo> fields = GetColumns(tableName);
			
			return new List<DataFieldInfo>(fields.Values);
		}

        public override Dictionary<Index, List<DataField>> GetIndexes(string tableName)
        {
            Dictionary<Index, List<DataField>> indexes = new Dictionary<Index, List<DataField>>(4);

            DataTable fieldTable = SelectQuery(string.Format("SHOW INDEXES FROM `{0}`",
                Mysql.Escape(tableName)));

            foreach (DataRow dr in fieldTable.Rows)
            {
                DataField df = new DataField((string)dr["Table"], (string)dr["Column_name"]);
                string key = (string)dr["Key_name"];
				
				Index index = null;
					
				if ((long)dr["Non_unique"] == 0)
				{
					if (key == "PRIMARY")
					{
						index = new PrimaryKey();
					}
					else
					{
						index = new UniqueKey(key);
					}
				}
				else
				{
					index = new Index(key);
				}

                if (indexes.ContainsKey(index) == false)
                {
                    indexes.Add(index, new List<DataField>());
                }

                indexes[index].Add(df);
            }

            return indexes;
        }

        private DataFieldInfo MysqlToType(string name, string type)
        {
            type = type.ToLower();

            bool unsigned = type.EndsWith("unsigned", StringComparison.Ordinal);
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
            string defaultValue = String.Empty;
            string notNull = String.Empty;

            if (type.ToLower() == "text" || type.ToLower() == "mediumtext" || type.ToLower() == "longtext")
            {
                notNull = " NOT NULL";
            }

            if (type.ToLower().Contains("int("))
            {
                if ((field.Key & DataFieldKeys.Primary) == DataFieldKeys.Primary)
                {
                    notNull = " NOT NULL";
                    if (Index.GetFields(field.PrimaryIndex.Key, GetFields(tableName)).Count == 1)
					{
						defaultValue = " DEFAULT NULL AUTO_INCREMENT";
					}
					else
					{
						defaultValue = " DEFAULT 0";
					}
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
                string defaultValue = String.Empty;
                string notNull = String.Empty;

                if (type.ToLower() == "text" || type.ToLower() == "mediumtext" || type.ToLower() == "longtext")
                {
                    notNull = " NOT NULL";
                }

                if (type.ToLower().Contains("int("))
                {
                    if ((field.Key & DataFieldKeys.Primary) == DataFieldKeys.Primary)
                    {
                        notNull = " NOT NULL";
                        if (Index.GetFields(field.PrimaryIndex.Key, GetFields(tableName)).Count == 1)
						{
							defaultValue = " DEFAULT NULL AUTO_INCREMENT";
						}
						else
						{
							defaultValue = " DEFAULT 0";
						}
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
            string defaultValue = String.Empty;
            string notNull = String.Empty;

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
                if ((field.Key & DataFieldKeys.Primary) == DataFieldKeys.Primary)
                {
                    notNull = " NOT NULL";
                    if (Index.GetFields(field.PrimaryIndex.Key, GetFields(tableName)).Count == 1)
                    {
						defaultValue = " DEFAULT NULL AUTO_INCREMENT";
					}
					else
					{
						defaultValue = " DEFAULT 0";
					}
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

            string query = string.Format(@"ALTER TABLE `{0}` MODIFY COLUMN `{1}` {2}{3}{4};",
                Mysql.Escape(tableName), Mysql.Escape(field.Name), type, notNull, defaultValue);

            try
            {
                UpdateQuery(query);
            }
            catch (Exception ex)
            {
                // DEBUG HERE
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
            DataFieldInfo primaryKey = null;

            Console.WriteLine("Creating table: " + tableName);
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
                string notNull = String.Empty;
                string key = String.Empty;
                string defaultValue = String.Empty;

                if (type == "text" || type == "mediumtext" || type == "longtext")
                {
                    notNull = " NOT NULL";
                }

                if (type.ToLower().Contains("int("))
                {
                    if ((field.Key & DataFieldKeys.Primary) == DataFieldKeys.Primary)
                    {
                        notNull = " NOT NULL";
                        if (Index.GetFields(field.PrimaryIndex.Key, fields).Count == 1)
                        {
                            defaultValue = "";
                            key = " AUTO_INCREMENT";
                        }
                        else
                        {
                            defaultValue = " DEFAULT 0";
                        }
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

                if (field.Key > DataFieldKeys.None)
                {
                    if ((field.Key & DataFieldKeys.Primary) == DataFieldKeys.Primary)
                    {
                        if (Index.GetFields(field.PrimaryIndex.Key, fields).Count == 1)
						{
							notNull = " NOT NULL";
	                        primaryKey = field;
                            if (type.ToLower().Contains("int("))
                            {
                                key = " AUTO_INCREMENT";
                            }
						}
                    }
                }

                sb.Append(string.Format("`{0}` {1}{2}{3}{4}",
                    Mysql.Escape(field.Name), type, notNull, defaultValue, key));
            }

            List<Index> keys = Index.GetIndexes(fields);

            foreach (Index key in keys)
            {
                List<DataFieldInfo> keyFields = Index.GetFields(key.Key, fields);

				switch (key.KeyType)
				{
					case DataFieldKeys.Primary:
					    sb.Append(", PRIMARY KEY (");
						break;
					case DataFieldKeys.Unique:
					    sb.Append(string.Format(", UNIQUE `{0}` (",
					                        Mysql.Escape(key.Key)));
						break;
					case DataFieldKeys.Index:
					    sb.Append(string.Format(", KEY `{0}` (",
					                        Mysql.Escape(key.Key)));
						break;
				}

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

            sb.Append(") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4");

            try
            {
                Console.WriteLine(sb.ToString());
                UpdateQuery(sb.ToString());
            }
            catch (MySql.Data.MySqlClient.MySqlException ex)
            {
                Console.WriteLine("Mysql error in query:");
                Console.WriteLine(ex.ToString());
            }
        }

        public override void UpdateTableKeys(string tableName, List<DataFieldInfo> fields)
        {
            Dictionary<Index, List<DataField>> indexes = GetIndexes(tableName);
            List<Index> keys = Index.GetIndexes(fields);

            foreach (Index key in indexes.Keys)
            {
                bool removeKey = (!keys.Contains(key));
 
                if (removeKey)
                {
                    // delete the key
                    string sql = string.Format("ALTER TABLE `{0}` DROP INDEX `{1}`",
                        tableName, key);

                    try
                    {
                        UpdateQuery(sql);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Table: " + tableName);
                        Console.WriteLine("Query: " + sql);
                        foreach (DataFieldInfo field in fields)
                        {
                            Console.WriteLine("  Field: " + field.Name);
                        }
                        throw ex;
                    }
                }
            }

            foreach (Index key in keys)
            {
                List<DataFieldInfo> keyFields = UniqueKey.GetFields(key.Key, fields);
                bool newKey = (!indexes.ContainsKey(key));

                if (newKey)
                {
                    // create the key
                    bool first = true;
                    string fieldList = String.Empty;
                    
                    foreach (DataFieldInfo keyField in keyFields)
                    {
                        if (!first)
                        {
                            fieldList += ", ";
                        }
                        first = false;

                        fieldList += "`" + keyField.Name + "`";
                    }

                    string sql = String.Empty;

					switch (key.KeyType)
					{
						case DataFieldKeys.Primary:
							sql = string.Format("ALTER TABLE `{0}` ADD PRIMARY KEY ({1});",
	                        tableName, fieldList);
							break;
						case DataFieldKeys.Unique:						
							sql = string.Format("ALTER TABLE `{0}` ADD UNIQUE INDEX `{1}` ({2});",
	                        tableName, key, fieldList);
							break;
						case DataFieldKeys.Index:
							sql = string.Format("ALTER TABLE `{0}` ADD KEY `{1}` ({2});",
	                        tableName, key, fieldList);
							break;
					}

                    UpdateQuery(sql);
                }
            }
        }
    }
}
