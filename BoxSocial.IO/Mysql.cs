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

using System.Data;
using System.Data.OleDb;
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

        public void iQuery()
        {
            throw new System.NotImplementedException();
        }

        public new DataTable SelectQuery(string sqlquery)
        {
            queryCount++;
            sqlquery = sqlquery.Replace("\\", "\\\\");
            DataTable resultTable;
            try
            {
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

        public DataTable SelectQuery(SelectQuery query)
        {
            return SelectQuery(query.ToString());
        }

        public new long UpdateQuery(string sqlquery)
        {
            return this.UpdateQuery(sqlquery, false);
        }

        public long UpdateQuery(Query query)
        {
            return UpdateQuery(query.ToString());
        }

        public long UpdateQuery(Query query, bool startTransaction)
        {
            return UpdateQuery(query.ToString(), startTransaction);
        }

        /// <summary>
        /// On an INSERT INTO query returns the ID of the inserted query. On an update it returns number of rows Affected. On Error returns -1.
        /// </summary>
        /// <param name="sqlquery"></param>
        /// <returns></returns>
        public long UpdateQuery(string sqlquery, bool startTransaction)
        {
            int rowsAffected = 0;
            queryCount++;

            if (sqlCommand == null)
            {
                sqlCommand = sqlConnection.CreateCommand();
            }

            if (!inTransaction && startTransaction)
            {
                sqlTransaction = sqlConnection.BeginTransaction();
                inTransaction = true;
            }

            try
            {
                sqlCommand.CommandText = sqlquery;
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
                        throw new System.Exception(ex.ToString());
                    }
                }
                catch (MySql.Data.MySqlClient.MySqlException me)
                {
                    return TRANSACTION_ROLLBACK_FAIL; // failed rollback
                }
                return TRANSACTION_ROLLBACK; // rollback
            }

            if (inTransaction && !startTransaction)
            {
                sqlTransaction.Commit();
                inTransaction = false;
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

        private void BeginTransaction()
        {
            if (!inTransaction)
            {
                sqlTransaction = sqlConnection.BeginTransaction();
                inTransaction = true;
            }
        }

        private bool ComittTransaction()
        {
            inTransaction = false;
            try
            {
                sqlTransaction.Commit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void RollBackTransaction()
        {
            sqlTransaction.Rollback();
        }

        public void CloseConnection()
        {
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
    }
}