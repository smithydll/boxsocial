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
using System.Web.Caching;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public sealed class NumberedItemsCache
    {
        private Core core;
        private Mysql db;

        /// <summary>
        /// List of types accessed
        /// </summary>
        private Dictionary<long, Type> typesAccessed = new Dictionary<long, Type>(16);

        /// <summary>
        /// A cache of items loaded.
        /// </summary>
        private Dictionary<NumberedItemId, NumberedItem> itemsCached = new Dictionary<NumberedItemId, NumberedItem>(128);
        private static Object itemsPersistedLock = new object();
        private static Dictionary<NumberedItemId, NumberedItem> itemsPersisted = null;

        /// <summary>
        /// A list of item Ids for batched loading
        /// </summary>
        private List<NumberedItemId> batchedItemIds = new List<NumberedItemId>(20);

        public NumberedItemsCache(Core core)
        {
            this.core = core;
            this.db = core.Db;

            if (itemsPersisted == null)
            {
                object o = null;
                System.Web.Caching.Cache cache;

                if (HttpContext.Current != null && HttpContext.Current.Cache != null)
                {
                    cache = HttpContext.Current.Cache;
                }
                else
                {
                    cache = new Cache();
                }

                try
                {
                    if (cache != null)
                    {
                        o = cache.Get("NumberedItems");
                    }
                }
                catch (NullReferenceException)
                {
                }

                lock (itemsPersistedLock)
                {
                    if (o != null && o is Dictionary<NumberedItemId, NumberedItem>)
                    {
                        itemsPersisted = (Dictionary<NumberedItemId, NumberedItem>)o;
                    }
                    else
                    {
                        itemsPersisted = new Dictionary<NumberedItemId, NumberedItem>(32);
                    }
                }
            }

            if (itemsPersisted != null)
            {
                lock (itemsPersistedLock)
                {
                    foreach (NumberedItemId nii in itemsPersisted.Keys)
                    {
                        itemsCached.Add(nii, itemsPersisted[nii]);
                    }
                }
            }
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
            try
            {
                long id = item.Id;
            }
            catch (NotImplementedException)
            {
                // Cannot cache this item
            }
            NumberedItemId itemKey = new NumberedItemId(item.Id, item.ItemKey.TypeId);

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

                    typesAccessed.Add(itemKey.TypeId, ae.Assembly.GetType(itemKey.TypeString));
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

            if (!(itemsCached.ContainsKey(itemKey)))
            {
                itemsCached.Add(itemKey, item);

                if (itemKey.TypeId == ItemType.GetTypeId(typeof(ApplicationEntry)))
                {
                    ApplicationEntry ae = (ApplicationEntry)item;

                    /* The Prose should auto load because it is lightweight and so important */
                    if (ae.Id > 0)
                    {
                        if (core.Prose != null)
                        {
                            core.Prose.AddApplication(ae.Key);
                        }
                    }
                }
            }

            Type typeToGet;

            typeToGet = typesAccessed[itemKey.TypeId];

            if (typeToGet != null && itemsPersisted != null && typeToGet.GetCustomAttributes(typeof(CacheableAttribute), false).Length > 0)
            {
                lock (itemsPersistedLock)
                {
                    if (!(itemsPersisted.ContainsKey(itemKey)))
                    {
                        itemsPersisted.Add(itemKey, item);
                    }
                }
            }
            batchedItemIds.Remove(itemKey);
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

                    typesAccessed.Add(itemKey.TypeId, ae.Assembly.GetType(itemKey.TypeString));
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
            List<long> itemId = new List<long>(20);

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
                    // this may seem counter intuitive, but the items self-cache through the RegisterItem(NumberedItem) method
                    NumberedItem item = Activator.CreateInstance(typeToGet, new object[] { core, dr }) as NumberedItem;

                    NumberedItemId loadedId = new NumberedItemId(item.Id, typeToGet);
                    batchedItemIds.Remove(loadedId);
                }

                if (itemsPersisted != null)
                {
                    System.Web.Caching.Cache cache;

                    if (HttpContext.Current != null && HttpContext.Current.Cache != null)
                    {
                        cache = HttpContext.Current.Cache;
                    }
                    else
                    {
                        cache = new Cache();
                    }

                    if (cache != null)
                    {
                        try
                        {
                            cache.Add("NumberedItems", itemsPersisted, null, Cache.NoAbsoluteExpiration, new TimeSpan(1, 0, 0), CacheItemPriority.Default, null);
                        }
                        catch (NullReferenceException)
                        {
                        }
                    }
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

        public void ExecuteQueue()
        {
            int count = 0; // it is advisable to avoid an infinite loop, and to avoid more than 100 items on a page
            while (batchedItemIds.Count > 0 && count < 100)
            {
                loadBatchedIds(batchedItemIds[0].TypeId, batchedItemIds[0].Id);
                count++;
            }
        }

        public void FlushQueue()
        {
            batchedItemIds.Clear();
        }
    }
}
