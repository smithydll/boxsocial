/*
 * Box Social�
 * http://boxsocial.net/
 * Copyright � 2007, David Lachlan Smith
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
            return input.Replace("'", "''").Replace("\\", "\\\\").Replace("�", "\\�");
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

        /*public override long Query(InsertQuery query, bool transaction)
        {
            return UpdateQuery(query.ToString(), transaction);
        }

        public override long Query(UpdateQuery query, bool transaction)
        {
            return UpdateQuery(query.ToString(), transaction);
        }

        public override long Query(DeleteQuery query, bool transaction)
        {
            return UpdateQuery(query.ToString(), transaction);
        }*/

        public override DataTable Query(string query)
        {
            return SelectQuery(query);
        }
    }
}