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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public abstract class NumberedItem : Item
    {
        protected ItemKey key = null;

        protected NumberedItem(Core core)
            : base (core)
        {
        }

        protected NumberedItem(Core core, long id)
            : base(core)
        {
        }

        public abstract long Id
        {
            get;
        }

        public ItemKey ItemKey
        {
            get
            {
                if (key == null)
                {
                    key = new ItemKey(Id, this.GetType().FullName);
                }
                return key;
            }
        }

        protected List<Item> getSubItems(Type typeToGet)
        {
            return getSubItems(typeToGet, 0, 0);
        }

        protected List<Item> getSubItems(Type typeToGet, bool feedParentArgument)
        {
            return getSubItems(typeToGet, 0, 0, feedParentArgument);
        }

        protected List<Item> getSubItems(Type typeToGet, int currentPage, int perPage)
        {
            return getSubItems(typeToGet, currentPage, perPage, false);
        }

        protected List<Item> getSubItems(Type typeToGet, int currentPage, int perPage, bool feedParentArgument)
        {
            return getSubItems(typeToGet, currentPage, perPage, feedParentArgument, null, false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeToGet"></param>
        /// <param name="currentPage"></param>
        /// <param name="perPage"></param>
        /// <param name="feedParentArgument">Feed the parent as an argument to the constructor</param>
        /// <param name="sortColumn"></param>
        /// <param name="sortAsc"></param>
        /// <returns></returns>
        protected List<Item> getSubItems(Type typeToGet, int currentPage, int perPage, bool feedParentArgument, string sortColumn, bool sortAsc)
        {
            List<Item> items = new List<Item>();

            SelectQuery query;

            if (typeToGet.GetMethod(typeToGet.Name + "_GetSelectQueryStub", new Type[] { typeof(Core) }) != null)
            {
                query = (SelectQuery)typeToGet.InvokeMember(typeToGet.Name + "_GetSelectQueryStub", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { core }); //GetSelectQueryStub(typeToGet);
            }
            else if (typeToGet.GetMethod(typeToGet.Name + "_GetSelectQueryStub", Type.EmptyTypes) != null)
            {
                query = (SelectQuery)typeToGet.InvokeMember(typeToGet.Name + "_GetSelectQueryStub", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { }); //GetSelectQueryStub(typeToGet);
            }
            else
            {
                query = Item.GetSelectQueryStub(typeToGet);
            }

            if (!string.IsNullOrEmpty(sortColumn))
            {
                if (sortAsc)
                {
                    query.AddSort(SortOrder.Ascending, sortColumn);
                }
                else
                {
                    query.AddSort(SortOrder.Descending, sortColumn);
                }
            }

            if (perPage > 0)
            {
                query.LimitStart = (currentPage - 1) * perPage;
                query.LimitCount = perPage;
            }

            query.AddCondition(Item.GetTable(typeToGet) + "." + Item.GetParentField(this.GetType(), typeToGet), Id);

            DataTable itemsTable = db.Query(query);

            foreach (DataRow dr in itemsTable.Rows)
            {
                if (feedParentArgument)
                {
                    items.Add(Activator.CreateInstance(typeToGet, new object[] { core, this, dr }) as Item);
                }
                else
                {
                    items.Add(Activator.CreateInstance(typeToGet, new object[] { core, dr }) as Item);
                }
            }

            return items;
        }

        public long Delete()
        {
            return Delete(false);
        }

        public new long Delete(bool parentDeleted)
        {
            if (this is IPermissibleItem)
            {
                IPermissibleItem iThis = (IPermissibleItem)this;
                if (!iThis.Access.Can("DELETE"))
                {
                    throw new UnauthorisedToDeleteItemException();
                }
            }

            AuthenticateAction(ItemChangeAction.Delete);

            db.BeginTransaction();

            List<Type> subTypes = getSubTypes();
            foreach (Type subType in subTypes)
            {
                List<Item> subItems = getSubItems(subType);

                foreach (Item item in subItems)
                {
                    item.Delete(true);
                }
            }

            return base.Delete();
        }

        public static NumberedItem Reflect(Core core, ItemKey ik)
        {
            ItemType type = new ItemType(core, ik.TypeId);
            Type tType = null;
            
            if (type.ApplicationId > 0)
            {
                ApplicationEntry ae = new ApplicationEntry(core, type.ApplicationId);
    
                //Application a = BoxSocial.Internals.Application.GetApplication(core, AppPrimitives.Any, ae);
                string assemblyPath;
                if (ae.IsPrimitive)
                {
                    if (core.Http != null)
                    {
                        assemblyPath = Path.Combine(core.Http.AssemblyPath, string.Format("{0}.dll", ae.AssemblyName));
                    }
                    else
                    {
                        assemblyPath = string.Format("/var/www/bin/{0}.dll", ae.AssemblyName);
                    }
                }
                else
                {
                    if (core.Http != null)
                    {
                        assemblyPath = Path.Combine(core.Http.AssemblyPath, Path.Combine("applications", string.Format("{0}.dll", ae.AssemblyName)));
                    }
                    else
                    {
                        assemblyPath = string.Format("/var/www/bin/applications/{0}.dll", ae.AssemblyName);
                    }
                }
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                
                tType = assembly.GetType(type.TypeNamespace);
            }
            else
            {
                tType = Type.GetType(type.TypeNamespace);
            }

            return (Activator.CreateInstance(tType, new object[] { core, ik.Id }) as NumberedItem);
        }
    }
}
