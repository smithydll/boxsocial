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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Reflection;

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

        public abstract DataTable Query(SelectQuery query);
        public abstract long Query(InsertQuery query);
        public abstract long Query(UpdateQuery query);
        public abstract long Query(DeleteQuery query);

        public abstract DataTable Query(string query);

        public abstract long UpdateQuery(string query);

        public int GetQueryCount()
        {
            return queryCount;
        }

        public abstract Dictionary<string, DataFieldInfo> GetColumns(string tableName);

        public abstract void AddColumn(string tableName, DataFieldInfo field);
        public abstract void ChangeColumn(string tableName, DataFieldInfo field);

        public abstract void CreateTable(string tableName, List<DataFieldInfo> fields);
    }
}