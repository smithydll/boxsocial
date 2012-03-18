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
using System.Web.Caching;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
	public class ItemKey
	{
        private static Object itemTypeCacheLock = new object();
		private static Dictionary<string, long> itemTypeCache = null;
        private static Dictionary<string, long> itemApplicationCache = null;
		private long itemId;
		private long itemTypeId;
        private long applicationId;
		private string itemType;
        private Type type;
		
		public long Id
		{
			get
			{
				return itemId;
			}
		}
		
		public long TypeId
		{
			get
			{
				return itemTypeId;
			}
		}

        public long ApplicationId
        {
            get
            {
                return applicationId;
            }
        }
		
		public string TypeString
		{
			get
			{
				return itemType;
			}
		}

        public Type Type
        {
            get
            {
                return type;
            }
        }
		
		public ItemKey(long itemId, string itemType)
		{
			this.itemId = itemId;
			this.itemType = itemType;
            lock (itemTypeCacheLock)
            {
                this.itemTypeId = itemTypeCache[itemType];
                this.applicationId = itemApplicationCache[itemType];
            }
            this.type = null;
		}
		
		public ItemKey(long itemId, long itemTypeId)
		{
			this.itemId = itemId;
            lock (itemTypeCacheLock)
            {
                foreach (string value in itemTypeCache.Keys)
                {
                    if (itemTypeCache[value] == itemTypeId)
                    {
                        this.itemType = value;
                        break;
                    }
                }
                this.itemTypeId = itemTypeId;
                this.applicationId = itemApplicationCache[this.itemType];
            }
            this.type = null;
		}

        public ItemKey(long itemId, Type itemType)
        {
            this.itemId = itemId;
            long itemTypeId = GetTypeId(itemType);
            lock (itemTypeCacheLock)
            {
                foreach (string value in itemTypeCache.Keys)
                {
                    if (itemTypeCache[value] == itemTypeId)
                    {
                        this.itemType = value;
                        break;
                    }
                }
                this.itemTypeId = itemTypeId;
                this.applicationId = itemApplicationCache[this.itemType];
            }
            this.type = itemType;
        }
        
        public ItemKey(string key)
        {
            string[] keys = key.Split(new char[] {','});
            long itemId = long.Parse(keys[1]);
            long itemTypeId = long.Parse(keys[0]);
            
            this.itemId = itemId;
            lock (itemTypeCacheLock)
            {
                foreach (string value in itemTypeCache.Keys)
                {
                    if (itemTypeCache[value] == itemTypeId)
                    {
                        this.itemType = value;
                        break;
                    }
                }
                this.itemTypeId = itemTypeId;
                this.applicationId = itemApplicationCache[this.itemType];
            }
            this.type = null;
        }
		
		public static void populateItemTypeCache(Core core)
		{
            if (core == null)
            {
                throw new NullCoreException();
            }

            System.Web.Caching.Cache cache;
			object o = null;
            object b = null;
			
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
                    o = cache.Get("itemTypeIds");
                }
                catch (NullReferenceException)
                {
                }
                try
                {
                    b = cache.Get("itemApplicationIds");
                }
                catch (NullReferenceException)
                {
                }
			}

            lock (itemTypeCacheLock)
            {
                if (o != null && o.GetType() == typeof(System.Collections.Generic.Dictionary<string, long>))
                {
                    itemTypeCache = (Dictionary<string, long>)o;
                    itemApplicationCache = (Dictionary<string, long>)b;
                }
                else
                {
                    itemTypeCache = new Dictionary<string, long>();
                    itemApplicationCache = new Dictionary<string, long>();
                    SelectQuery query = ItemType.GetSelectQueryStub(typeof(ItemType));

                    DataTable typesTable;

                    try
                    {
                        typesTable = core.Db.Query(query);
                    }
                    catch
                    {
                        return;
                    }

                    foreach (DataRow dr in typesTable.Rows)
                    {
                        if (!(itemTypeCache.ContainsKey((string)dr["type_namespace"])))
                        {
                            itemTypeCache.Add((string)dr["type_namespace"], (long)dr["type_id"]);
                            itemApplicationCache.Add((string)dr["type_namespace"], (long)dr["type_application_id"]);
                        }
                    }

                    if (cache != null)
                    {
                        cache.Add("itemTypeIds", itemTypeCache, null, Cache.NoAbsoluteExpiration, new TimeSpan(4, 0, 0), CacheItemPriority.High, null);
                        cache.Add("itemApplicationIds", itemApplicationCache, null, Cache.NoAbsoluteExpiration, new TimeSpan(4, 0, 0), CacheItemPriority.High, null);
                    }
                }
            }
		}

        public static long GetTypeId(Type type)
        {
            if (itemTypeCache.ContainsKey(type.FullName))
            {
                return itemTypeCache[type.FullName];
            }
            else
            {
                return 0;
            }
        }
        
        public override string ToString ()
        {
            return string.Format("{0},{1}", TypeId, Id);
        }

        public override bool Equals(object obj)
        {
			if (obj == null)
				return false;
            if (obj.GetType() != typeof(ItemKey))
                return false;
            ItemKey ik = (ItemKey)obj;

            if (TypeId != ik.TypeId)
                return false;
            if (Id != ik.Id)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /*public static bool operator ==(ItemKey ik1, ItemKey ik2)
        {
			if ((ik1.Equals(null) || ik2.Equals(null)))
				return false;
            return ik1.Equals(ik2);
        }

        public static bool operator !=(ItemKey ik1, ItemKey ik2)
        {
			if ((ik1.Equals(null) && ik2.Equals(null)))
				return false;
			if (ik1.Equals(null))
				return true;
			if (ik2.Equals(null))
				return true;
			return (!ik1.Equals(ik2));
        }*/
	}
}
