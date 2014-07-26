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
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public class HibernateItem
    {
        private Dictionary<string, object> values;

        public HibernateItem(System.Data.Common.DbDataReader reader)
        {
            values = new Dictionary<string, object>(64, StringComparer.Ordinal);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string column = reader.GetName(i);

                values[column] = reader.GetValue(i);
            }
        }

        public DataRow GetDataRow()
        {
            DataTable table = new DataTable();

            foreach (string key in values.Keys)
            {
                DataColumn newColumn = table.Columns.Add(key);
                newColumn.DataType = values[key].GetType();
            }

            DataRow newRow = table.NewRow();

            foreach (string key in values.Keys)
            {
                newRow[key] = values[key];
            }

            return newRow;
        }

        public object this[string key]
        {
            get
            {
                return values[key];
            }
            set
            {
                values[key] = value;
            }
        }

        public bool ContainsKey(string key)
        {
            return values.ContainsKey(key);
        }
    }
}
