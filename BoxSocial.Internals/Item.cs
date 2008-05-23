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
using System.Data;
using System.Reflection;
using System.Diagnostics;
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public abstract class Item
    {
        public const long MYSQL_TEXT = 65535L;
        public const long MYSQL_MEDIUM_TEXT = 16777215L;
        public const long MYSQL_LONG_TEXT = 4294967295L;

        /// <summary>
        /// Database object
        /// </summary>
        protected Mysql db;

        /// <summary>
        /// Core token
        /// </summary>
        protected Core core;

        private List<string> updatedItems;

        public delegate void ItemLoadHandler();

        public event ItemLoadHandler ItemLoad;

        protected Item(Core core)
        {
            this.core = core;
            this.db = core.db;
            updatedItems = new List<string>();
        }

        protected void LoadItem(long primaryKey)
        {
            // 1. Discover primary key
            // 2. Build query
            // 3. Execute query
            // 4. Fill results

            string tableName = GetTable(this.GetType());
            List<DataFieldInfo> fields = GetFields(this.GetType());
            string keyField = "";

            SelectQuery query = new SelectQuery(tableName);

            foreach (DataFieldInfo field in fields)
            {
                if (field.IsPrimaryKey)
                {
                    keyField = field.Name;
                }

                query.AddFields(field.Name);
            }

            if (string.IsNullOrEmpty(keyField))
            {
                // Error
                throw new NoPrimaryKeyException();
            }

            query.AddCondition(keyField, primaryKey);

            //HttpContext.Current.Response.Write(query.ToString());

            DataTable itemTable = Query(query);

            if (itemTable.Rows.Count == 1)
            {
                loadItemInfo(itemTable.Rows[0]);

                if (ItemLoad != null)
                {
                    ItemLoad();
                }
            }
            else
            {
                // Error
                throw new InvalidItemException();
            }
        }

        protected void LoadItem(string uniqueIndex, object value)
        {
            // 1. check index is unique
            // 2. Build query
            // 3. Execute query
            // 4. Fill results

            string tableName = GetTable(this.GetType());
            List<DataFieldInfo> fields = GetFields(this.GetType());
            string keyField = "";

            SelectQuery query = new SelectQuery(tableName);

            foreach (DataFieldInfo field in fields)
            {
                if (field.Name == uniqueIndex)
                {
                    if (field.IsUnique)
                    {
                        keyField = field.Name;
                    }
                    else
                    {
                        throw new FieldNotUniqueIndexException();
                    }
                }

                query.AddFields(field.Name);
            }

            query.AddCondition(keyField, value);

            DataTable itemTable = Query(query);

            if (itemTable.Rows.Count == 1)
            {
                loadItemInfo(itemTable.Rows[0]);

                if (ItemLoad != null)
                {
                    ItemLoad();
                }
            }
            else
            {
                // Error
                throw new InvalidItemException();
            }
        }

        protected void UpdateThis()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);

            updatedItems.Add(sf.GetMethod().Name);

            HttpContext.Current.Response.Write(sf.GetMethod().Name);
        }

        protected void SetProperty(string key, object value)
        {
            try
            {
                Type thisType = this.GetType();
                FieldInfo fi = thisType.GetField(key, BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic);
                fi.SetValue(this, value);

                updatedItems.Add(key);
            }
            catch
            {
            }
        }

        protected DataTable Query(SelectQuery query)
        {
            return db.Query(query);
        }

        protected long Query(InsertQuery query)
        {
            return db.Query(query);
        }

        protected long Query(UpdateQuery query)
        {
            return db.Query(query);
        }

        protected long Query(DeleteQuery query)
        {
            return db.Query(query);
        }

        public abstract long Id
        {
            get;
        }

        public abstract string Namespace
        {
            get;
        }

        public abstract string Uri
        {
            get;
        }

        protected List<ItemTag> getTags()
        {
            List<ItemTag> tags = new List<ItemTag>();

            return tags;
        }

        internal protected static List<DataFieldInfo> GetFields(Type type)
        {
            List<DataFieldInfo> returnValue = new List<DataFieldInfo>();

            foreach (FieldInfo fi in type.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi))
                {
                    if (attr.GetType() == typeof(DataFieldAttribute))
                    {
                        if (((DataFieldAttribute)attr).FieldName != null)
                        {
                            DataFieldInfo dfi = new DataFieldInfo(((DataFieldAttribute)attr).FieldName, fi.FieldType, ((DataFieldAttribute)attr).MaxLength, ((DataFieldAttribute)attr).Indexes);

                            returnValue.Add(dfi);
                        }
                        else
                        {
                            returnValue.Add(new DataFieldInfo(fi.Name, fi.FieldType, 255));
                        }
                    }
                }

            }

            return returnValue;
        }

        internal protected static string GetTable(Type type)
        {
            bool attributeFound = false;
            foreach (Attribute attr in type.GetCustomAttributes(typeof(DataTableAttribute), false))
            {
                if (attr != null)
                {
                    if (((DataTableAttribute)attr).TableName != null)
                    {
                        return ((DataTableAttribute)attr).TableName;
                    }
                    attributeFound = true;
                }
            }

            if (attributeFound)
            {
                return type.Name;
            }
            else
            {
                return null;
            }
        }

        public long Update()
        {
            UpdateQuery uQuery = new UpdateQuery(Item.GetTable(this.GetType()));
            
            foreach (FieldInfo fi in this.GetType().GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi))
                {

                    if (updatedItems.Contains(fi.Name))
                    {
                        if (attr.GetType() == typeof(DataFieldAttribute))
                        {
                            if (((DataFieldAttribute)attr).FieldName != null)
                            {
                                uQuery.AddField(((DataFieldAttribute)attr).FieldName, fi.GetValue(this));
                            }
                            else
                            {
                                uQuery.AddField(fi.Name, fi.GetValue(this));
                            }
                        }
                    }
                }
            }

            return db.Query(uQuery);
        }

        protected void loadItemInfo(DataRow itemRow)
        {
            List<string> columns = new List<string>();

            foreach (DataColumn column in itemRow.Table.Columns)
            {
                columns.Add(column.ColumnName);
            }

            foreach (FieldInfo fi in this.GetType().GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi))
                {
                    if (attr.GetType() == typeof(DataFieldAttribute))
                    {
                        string columnName;
                        if (((DataFieldAttribute)attr).FieldName != null)
                        {
                            columnName = ((DataFieldAttribute)attr).FieldName;
                        }
                        else
                        {
                            columnName = fi.Name;
                        }

                        if (columns.Contains(columnName))
                        {
                            if (!(itemRow[columnName] is DBNull))
                            {
                                fi.SetValue(this, itemRow[columnName]);
                            }
                        }
                    }
                }

            }
        }
    }

    public class InvalidItemException : Exception
    {
    }

    public class NoPrimaryKeyException : Exception
    {
    }

    public class FieldNotUniqueIndexException : Exception
    {
    }
}
