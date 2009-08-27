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
    public class DataField
    {
        private string fieldName;
        private string tableName;
        private string alias;

        public string Name
        {
            get
            {
                return fieldName;
            }
        }

        public string Table
        {
            get
            {
                return tableName;
            }
        }

        public string Alias
        {
            get
            {
                return alias;
            }
        }
        
        public DataField(Type type, string fieldName)
        {
            this.tableName = DataFieldAttribute.GetTable(type);
            this.fieldName = fieldName;
        }

        public DataField(string tableName, string fieldName)
        {
            this.tableName = tableName;
            this.fieldName = fieldName;
        }

        public DataField(string tableName, string fieldName, string alias)
        {
            this.tableName = tableName;
            this.fieldName = fieldName;
            this.alias = alias;
        }

        public override string ToString()
        {
            if (alias != null)
            {
                return string.Format("`{0}`.`{1}` AS `{2}`",
                    Table, Name, Alias);
            }
            else
            {
                return string.Format("`{0}`.`{1}`",
                    Table, Name);
            }
        }
    }
}
