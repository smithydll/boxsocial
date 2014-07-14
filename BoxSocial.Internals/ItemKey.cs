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
using System.Reflection;
using System.Diagnostics;
using System.Text;
using System.Web;
using System.Web.Caching;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
	public class ItemKey : IComparable
	{
        private static Object itemTypeCacheLock = new object();
		private static Dictionary<string, ItemType> itemTypeCache = null;
        private static Dictionary<string, long> itemApplicationCache = null;
        private static Dictionary<long, ItemType> primitiveTypes = null;
		private long itemId;
		private long itemTypeId;
        private long applicationId;
		private string itemType;
        private Type type;
        private ItemType typeRow;
		
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
                if (type == null)
                {
                    type = Type.GetType(ItemKey.GetItemType(TypeId).TypeNamespace);
                }
                return type;
            }
        }

        public bool ImplementsLikeable
        {
            get
            {
                if (typeRow != null)
                {
                    return typeRow.Likeable;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool ImplementsShareable
        {
            get
            {
                if (typeRow != null)
                {
                    return typeRow.Shareable;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool ImplementsCommentable
        {
            get
            {
                if (typeRow != null)
                {
                    return typeRow.Commentable;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool ImplementsRateable
        {
            get
            {
                if (typeRow != null)
                {
                    return typeRow.Rateable;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool ImplementsSubscribeable
        {
            get
            {
                if (typeRow != null)
                {
                    return typeRow.Subscribeable;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool ImplementsViewable
        {
            get
            {
                if (typeRow != null)
                {
                    return typeRow.Viewable;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool ImplementsNotifiable
        {
            get
            {
                if (typeRow != null)
                {
                    return typeRow.Notifiable;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool ImplementsEmbeddable
        {
            get
            {
                if (typeRow != null)
                {
                    return typeRow.Embeddable;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool InheritsPrimitive
        {
            get
            {
                if (typeRow != null)
                {
                    return typeRow.IsPrimitive;
                }
                else
                {
                    return false;
                }
            }
        }
		
		public ItemKey(long itemId, string itemType)
		{
			this.itemId = itemId;
			this.itemType = itemType;
            lock (itemTypeCacheLock)
            {
                if (!itemTypeCache.TryGetValue(itemType, out this.typeRow))
                {
                    throw new Exception(string.Format("Cannot find key {0} in {1}", itemType, "itemTypeCache"));
                }
                if (!itemApplicationCache.TryGetValue(itemType, out this.applicationId))
                {
                    throw new Exception(string.Format("Cannot find key {0} in {1}", itemType, "itemApplicationCache"));
                }
                //this.typeRow = itemTypeCache[itemType];
                this.itemTypeId = this.typeRow.TypeId;
                //this.applicationId = itemApplicationCache[itemType];
            }
            this.type = null;
		}
		
		public ItemKey(long itemId, long itemTypeId)
		{
            if (itemTypeId < 1)
            {
                this.itemId = itemId;
                this.itemTypeId = 0;
                this.applicationId = 0;
                this.type = null;
                this.typeRow = null;
                return;
            }

			this.itemId = itemId;
            lock (itemTypeCacheLock)
            {
                foreach (string value in itemTypeCache.Keys)
                {
                    if (itemTypeCache[value].TypeId == itemTypeId)
                    {
                        this.itemType = value;
                        break;
                    }
                }
                this.typeRow = itemTypeCache[itemType];
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
                    if (itemTypeCache[value].TypeId == itemTypeId)
                    {
                        this.itemType = value;
                        break;
                    }
                }
                this.typeRow = itemTypeCache[this.itemType];
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
                    if (itemTypeCache[value].TypeId == itemTypeId)
                    {
                        this.itemType = value;
                        break;
                    }
                }
                this.typeRow = itemTypeCache[this.itemType];
                this.itemTypeId = itemTypeId;
                this.applicationId = itemApplicationCache[this.itemType];
            }
            this.type = null;
        }

        public static Dictionary<long, ItemType> PrimitiveTypes
        {
            get
            {
                lock (itemTypeCacheLock)
                {
                    if (primitiveTypes == null)
                    {
                        primitiveTypes = new Dictionary<long, ItemType>(8);
                        foreach (string key in itemTypeCache.Keys)
                        {
                            if (itemTypeCache[key].IsPrimitive)
                            {
                                primitiveTypes.Add(itemTypeCache[key].TypeId, itemTypeCache[key]);
                            }
                        }
                    }
                    return primitiveTypes;
                }
            }
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
                cache = new System.Web.Caching.Cache();
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
                if (o != null && o.GetType() == typeof(System.Collections.Generic.Dictionary<string, ItemType>))
                {
                    itemTypeCache = (Dictionary<string, ItemType>)o;
                    itemApplicationCache = (Dictionary<string, long>)b;
                }
                else
                {
                    itemTypeCache = new Dictionary<string, ItemType>(256, StringComparer.Ordinal);
                    itemApplicationCache = new Dictionary<string, long>(256, StringComparer.Ordinal);
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
                        ItemType typeItem = new ItemType(core, dr);
                        if (!(itemTypeCache.ContainsKey(typeItem.TypeNamespace)))
                        {
                            itemTypeCache.Add(typeItem.TypeNamespace, typeItem);
                            itemApplicationCache.Add(typeItem.TypeNamespace, typeItem.ApplicationId);
                        }
                    }

                    if (cache != null)
                    {
                        cache.Add("itemTypeIds", itemTypeCache, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(4, 0, 0), CacheItemPriority.High, null);
                        cache.Add("itemApplicationIds", itemApplicationCache, null, System.Web.Caching.Cache.NoAbsoluteExpiration, new TimeSpan(4, 0, 0), CacheItemPriority.High, null);
                    }
                }
            }
		}

        public static long GetTypeId(Type type)
        {
            ItemType it = null;

            if (itemTypeCache.TryGetValue(type.FullName, out it))
            {
                return it.TypeId;
            }
            else
            {
                return 0;
            }
        }

        public static ItemType GetItemType(long typeId)
        {
            ItemType it = null;

            foreach (ItemType type in itemTypeCache.Values)
            {
                if (type.Id == typeId)
                {
                    it = type;
                }
            }

            return it;
        }
        
        public override string ToString ()
        {
            return string.Format("Type: {0}:{1}, Id: {2}", TypeId, TypeString, Id);
        }

        public override bool Equals(object obj)
        {
            ItemKey ik = obj as ItemKey;
            if (ik == null) return false;

            if (TypeId != ik.TypeId)
                return false;
            if (Id != ik.Id)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return TypeId.GetHashCode() ^ Id.GetHashCode();
        }

        public int CompareTo(object obj)
        {
            ItemKey ik = obj as ItemKey;
            if (ik == null) return -1;


            int c = ik.TypeId.CompareTo(TypeId);
            if (c == 0)
            {
                return ik.Id.CompareTo(Id);
            }
            else
            {
                return c;
            }
        }
    }
}
