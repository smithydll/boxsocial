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
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.IO
{
    public enum DataTableTypes
    {
        NonVolatile = 0x00,
        Volatile = 0x01,
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DataTableAttribute : Attribute
    {
        private string tableName;
        private string tableNamespace;
        private DataTableTypes tableType;

        public DataTableAttribute(string tableName)
        {
            this.tableName = tableName;
        }

        public DataTableAttribute(string tableName, DataTableTypes tableType)
        {
            this.tableName = tableName;
            this.tableType = tableType;
        }

        public DataTableAttribute(string tableName, string tableNamespace)
        {
            this.tableName = tableName;
            this.tableNamespace = tableNamespace;
        }

        public string TableName
        {
            get
            {
                return tableName;
            }
        }

        public string Namespace
        {
            get
            {
                return tableNamespace;
            }
        }
    }
}
