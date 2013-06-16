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
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public sealed class NumberedItemsCache
    {
        private Core core;
        private Mysql db;

        public NumberedItemsCache(Core core)
        {
            this.core = core;
            this.db = core.Db;
        }

        public void RegisterType(Type type)
        {
            long typeId = ItemType.GetTypeId(type);
            if (!typesAccessed.ContainsKey(typeId))
            {
                typesAccessed.Add(typeId, type);
            }
        }

        public void RegisterItem(NumberedItem item)
        {
            NumberedItemId itemKey = new NumberedItemId(item.Id, item.ItemKey.TypeId);

            if (!(itemsCached.ContainsKey(itemKey)))
            {
                itemsCached.Add(itemKey, item);
            }
            if (batchedItemIds.Contains(itemKey))
            {
                batchedItemIds.Remove(itemKey);
            }
        }

        public void RequestItem(ItemKey itemKey)
        {
            if (itemKey.TypeId == 0)
            {
                return;
            }
            if (!typesAccessed.ContainsKey(itemKey.TypeId))
            {
                // We need to make sure that the application is loaded
                if (itemKey.ApplicationId > 0)
                {
                    core.ItemCache.RegisterType(typeof(ApplicationEntry));

                    ItemKey applicationKey = new ItemKey(itemKey.ApplicationId, typeof(ApplicationEntry));
                    core.ItemCache.RequestItem(applicationKey);

                    ApplicationEntry ae = (ApplicationEntry)core.ItemCache[applicationKey];

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

                    typesAccessed.Add(itemKey.TypeId, assembly.GetType(itemKey.TypeString));
                }
                else
                {
                    try
                    {
                        typesAccessed.Add(itemKey.TypeId, Type.GetType(itemKey.TypeString));
                    }
                    catch
                    {
                        HttpContext.Current.Response.Write(itemKey.ToString());
                        HttpContext.Current.Response.End();
                    }
                }
            }
            NumberedItemId loadedId = new NumberedItemId(itemKey.Id, itemKey.TypeId);
            if (!(batchedItemIds.Contains(loadedId) || itemsCached.ContainsKey(loadedId)))
            {
                batchedItemIds.Add(loadedId);
            }
        }

        /// <summary>
        /// List of types accessed
        /// </summary>
        private Dictionary<long, Type> typesAccessed = new Dictionary<long, Type>();

        /// <summary>
        /// A cache of items loaded.
        /// </summary>
        private Dictionary<NumberedItemId, NumberedItem> itemsCached = new Dictionary<NumberedItemId, NumberedItem>();

        /// <summary>
        /// A list of item Ids for batched loading
        /// </summary>
        private List<NumberedItemId> batchedItemIds = new List<NumberedItemId>();

        public NumberedItem this[ItemKey key]
        {
            get
            {
                if (key.TypeId == 0)
                {
                    return null;
                }
                loadBatchedIds(key.TypeId, key.Id);
                NumberedItem item = itemsCached[new NumberedItemId(key.Id, key.TypeId)];
                return item;
            }
        }

        public bool ContainsItem(ItemKey key)
        {
            return itemsCached.ContainsKey(new NumberedItemId(key.Id, key.TypeId));
        }

        private void loadItems(long typeId, List<NumberedItemId> itemIds)
        {
            List<long> itemId = new List<long>();

            foreach (NumberedItemId id in itemIds)
            {
                if (id.TypeId == typeId)
                {
                    itemId.Add(id.Id);
                }
            }

            Type typeToGet;

            typeToGet = typesAccessed[typeId];

            if (typeToGet != null)
            {
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

                query.AddCondition(Item.GetTable(typeToGet) + "." + Item.GetPrimaryKey(typeToGet), ConditionEquality.In, itemId);

                DataTable itemsTable = db.Query(query);

                foreach (DataRow dr in itemsTable.Rows)
                {
                    NumberedItem item = Activator.CreateInstance(typeToGet, new object[] { core, dr }) as NumberedItem;

                    NumberedItemId loadedId = new NumberedItemId(item.Id, typeToGet);
                    itemsCached.Add(loadedId, item);
                    batchedItemIds.Remove(loadedId);
                }
            }
        }

        private void loadBatchedIds(long typeId, long requestedId)
        {
            if (batchedItemIds.Contains(new NumberedItemId(requestedId, typeId)))
            {
                loadItems(typeId, batchedItemIds);
            }
        }
    }
}
