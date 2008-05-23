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
                sqlTransaction.Dispose();
            }
            if (sqlCommand != null)
            {
                sqlCommand.Dispose();
            }
            sqlConnection.Close();
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

        public bool TableExists(string tableName)
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
                tableName));

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

        private DataFieldInfo MysqlToType(string name, string type)
        {
            type = type.ToLower();

            if (type == "tinyint(1)" || type == "tinyint(1) unsigned")
            {
                return new DataFieldInfo(name, typeof(bool), 0);
            }

            bool unsigned = type.EndsWith("unsigned");
            long length = 0;
            int indexOpenBracket = type.IndexOf("(");
            int indexCloseBracket = type.IndexOf(")");

            if (indexOpenBracket >= 0 && indexCloseBracket > indexOpenBracket)
            {
                length = long.Parse(type.Substring(indexOpenBracket + 1, indexCloseBracket - indexOpenBracket - 1));
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
                case "tiny":
                    if (unsigned)
                    {
                        return new DataFieldInfo(name, typeof(byte), length);
                    }
                    else
                    {
                        return new DataFieldInfo(name, typeof(sbyte), length);
                    }
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
                    return "tinyint(1)";
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
            else
            {
                return "varchar(255)";
            }
        }

        public override void AddColumn(string tableName, DataFieldInfo field)
        {
            string type = TypeToMysql(field);
            string notNull = "";

            if (type == "text" || type =="mediumtext"|| type=="longtext")
            {
                notNull = " NOT NULL";
            }

            string query = string.Format(@"ALTER TABLE `{0}` ADD COLUMN `{1}` {2}{3};",
                tableName, field.Name, type, notNull);

            UpdateQuery(query);
        }

        public override void ChangeColumn(string tableName, DataFieldInfo field)
        {
            string type = TypeToMysql(field);
            string notNull = "";

            if (type == "text" || type == "mediumtext" || type == "longtext")
            {
                notNull = " NOT NULL";
            }

            string query = string.Format(@"ALTER TABLE `{0}` MODIFY COLUMN `{1}` {2}{3};",
                tableName, field.Name, type, notNull);

            UpdateQuery(query);
        }


        public override void CreateTable(string tableName, List<DataFieldInfo> fields)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder indexes = new StringBuilder();
            DataFieldInfo primaryKey = null;

            sb.Append(string.Format("CREATE TABLE IF NOT EXISTS `{0}` (",
                tableName));

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

                if (type == "text" || type == "mediumtext" || type == "longtext")
                {
                    notNull = " NOT NULL";
                }

                if (field.IsUnique)
                {
                    if (field.IsPrimaryKey)
                    {
                        primaryKey = field;
                        key = " NOT NULL AUTO_INCREMENT";
                        indexes.Append(string.Format(", PRIMARY_KEY(`{0}`)",
                            field.Name));
                    }
                    else
                    {
                        key = " NOT NULL UNIQUE KEY";
                        indexes.Append(string.Format(", INDEX(`{0}`)",
                            field.Name));
                    }
                }

                sb.Append(string.Format("`{0}` {1}{2}{3}",
                    field.Name, type, notNull, key));
            }

            if (primaryKey != null)
            {
                sb.Append(string.Format(", PRIMARY KEY (`{0}`)",
                    primaryKey.Name));
            }

            sb.Append(") ENGINE=InnoDB DEFAULT CHARSET=utf8");

            //HttpContext.Current.Response.Write(sb.ToString());

            UpdateQuery(sb.ToString());
        }
    }
}