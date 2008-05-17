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
        /// <summary>
        /// Database object
        /// </summary>
        protected Mysql db;

        /// <summary>
        /// Core token
        /// </summary>
        protected Core core;

        private List<string> updatedItems;

        protected Item(Core core)
        {
            this.core = core;
            this.db = core.db;
            updatedItems = new List<string>();
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

        protected static string[] GetFields(Type type)
        {
            List<string> returnValue = new List<string>();

            foreach (FieldInfo fi in type.GetFields(BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                foreach (Attribute attr in Attribute.GetCustomAttributes(fi))
                {
                    if (attr.GetType() == typeof(DataFieldAttribute))
                    {
                        if (((DataFieldAttribute)attr).FieldName != null)
                        {
                            returnValue.Add(((DataFieldAttribute)attr).FieldName);
                        }
                        else
                        {
                            returnValue.Add(fi.Name);
                        }
                    }
                }

            }

            return returnValue.ToArray();
        }

        protected static string GetTable(Type type)
        {
            foreach (Attribute attr in type.GetCustomAttributes(typeof(DataTableAttribute), false))
            {
                if (attr != null)
                {
                    if (((DataTableAttribute)attr).TableName != null)
                    {
                        return ((DataTableAttribute)attr).TableName;
                    }
                }
            }

            return type.Name;
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
    }
}
