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

namespace BoxSocial.IO
{
    public abstract class Database
    {
        protected string connectionString;
        protected int queryCount;

        public Database()
        {
            queryCount = 0;
        }

        public Database(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public string ConnectionString
        {
            get
            {
                return this.connectionString;
            }
            set
            {
                this.connectionString = value;
            }
        }

        protected void Connect()
        {
            throw new System.NotImplementedException();
        }

        public void iQuery(string sqlquery)
        {
            throw new System.NotImplementedException();
        }

        public DataTable Query(string sqlquery)
        {
            throw new System.NotImplementedException();
        }

        public DataTable SelectQuery(string sqlquery)
        {
            throw new System.NotImplementedException();
        }

        public long UpdateQuery(string sqlquery)
        {
            throw new System.NotImplementedException();
        }

        public int GetQueryCount()
        {
            return queryCount;
        }
    }
}