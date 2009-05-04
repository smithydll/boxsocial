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
		private static Dictionary<string, long> itemTypeCache;
		private long itemId;
		private long itemTypeId;
		private string itemType;
		
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
		
		public string Type
		{
			get
			{
				return itemType;
			}
		}
		
		public ItemKey(long itemId, string itemType)
		{
			this.itemId = itemId;
			this.itemType = itemType;
			this.itemTypeId = itemTypeCache[itemType];
		}
		
		public ItemKey(long itemId, long itemTypeId)
		{
			this.itemId = itemId;
			foreach (string value in itemTypeCache.Keys)
			{
				if (itemTypeCache[value] == itemTypeId)
				{
					this.itemType = value;
					break;
				}
			}
			this.itemTypeId = itemTypeId;
		}
		
		public static void populateItemTypeCache(Core core)
		{
            System.Web.Caching.Cache cache;
			object o = null;
			
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
				o = cache.Get("itemTypeIds");
			}
			
			if (o != null && o.GetType() == typeof(System.Collections.Generic.Dictionary<string, long>))
			{
				itemTypeCache = (Dictionary<string, long>)o;
			}
			else
			{
				itemTypeCache = new Dictionary<string,long>();
				SelectQuery query = ItemType.GetSelectQueryStub(typeof(ItemType));
				
				DataTable typesTable;
				
				try
				{
					typesTable = core.db.Query(query);
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
                    }
				}

				if (cache != null)
				{
					cache.Add("itemTypeIds", itemTypeCache, null, Cache.NoAbsoluteExpiration, new TimeSpan(4, 0, 0), CacheItemPriority.High, null);
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

        /*public override bool Equals(object obj)
        {
			if (obj.Equals(null))
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

        public static bool operator ==(ItemKey ik1, ItemKey ik2)
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