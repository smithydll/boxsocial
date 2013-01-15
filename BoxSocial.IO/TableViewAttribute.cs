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
using System.Collections.Generic;
using System.Text;

namespace BoxSocial.IO
{

    [AttributeUsage(AttributeTargets.Class)]
    public class TableViewAttribute : Attribute
    {
        private string tableName;
        private string tableNamespace;

        public TableViewAttribute(string tableName)
        {
            this.tableName = tableName;
        }


        public TableViewAttribute(string tableName, string tableNamespace)
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
