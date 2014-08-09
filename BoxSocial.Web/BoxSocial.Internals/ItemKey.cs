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
		private long itemId;
		private long itemTypeId;
        private int hashCode;

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

        public ItemType GetType(Core core)
        {
            return ItemType.GetType(core, this);
        }

		public ItemKey(long itemId, long itemTypeId)
		{
            if (itemTypeId < 1)
            {
                this.itemId = itemId;
                this.itemTypeId = 0;
                return;
            }

			this.itemId = itemId;
            this.itemTypeId = itemTypeId;

            hashCode = TypeId.GetHashCode() ^ Id.GetHashCode();
		}

        public ItemKey(string key)
        {
            string[] keys = key.Split(new char[] { ',' });
            long itemId = long.Parse(keys[1]);
            long itemTypeId = long.Parse(keys[0]);

            this.itemId = itemId;
            this.itemTypeId = itemTypeId;

            hashCode = TypeId.GetHashCode() ^ Id.GetHashCode();
        }

        public static Dictionary<long, ItemType> GetPrimitiveTypes(Core core)
        {
            Dictionary<long, ItemType> primitiveTypes = null;
            object o = core.Cache.GetCached("itemPrimitiveTypes");
            Dictionary<long, string> primitiveTypesnames = null;

            if (!(o != null && o is Dictionary<long, string>))
            {
                primitiveTypesnames = (Dictionary<long, string>)o;
            }
            else
            {
                primitiveTypesnames = populateItemTypeCache(core);
            }

            if (primitiveTypesnames != null)
            {
                primitiveTypes = new Dictionary<long, ItemType>();

                foreach (long id in primitiveTypesnames.Keys)
                {
                    object p = core.Cache.GetCached(string.Format("itemTypes[{0}]", id));
                    if (p != null && p is HibernateItem)
                    {
                        primitiveTypes.Add(id, new ItemType(core, (HibernateItem)p));
                    }
                    else
                    {
                        primitiveTypes.Add(id, updateItemTypeCache(core, id));
                    }
                }
            }

            return primitiveTypes;
        }

        public static Dictionary<long, string> populateItemTypeCache(Core core)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Dictionary<long, string> primitiveTypes = new Dictionary<long, string>();

            object o = core.Cache.GetCached("itemPrimitiveTypes");

            if (o == null)
            {

                if (core.Cache != null)
                {
                    SelectQuery query = ItemType.GetSelectQueryStub(core, typeof(ItemType));

                    System.Data.Common.DbDataReader typesReader = null;

                    try
                    {
                        typesReader = core.Db.ReaderQuery(query);
                    }
                    catch
                    {
                        if (typesReader != null)
                        {
                            typesReader.Close();
                            typesReader.Dispose();
                        }

                        return primitiveTypes;
                    }

                    while (typesReader.Read())
                    {
                        ItemType typeItem = new ItemType(core, typesReader);
                        HibernateItem typeItemHibernate = new HibernateItem(typesReader);

                        core.Cache.SetCached(string.Format("itemTypes[{0}]", typeItem.Id), typeItemHibernate, new TimeSpan(4, 0, 0), CacheItemPriority.NotRemovable);
                        core.Cache.SetCached(string.Format("itemTypeIds[{0}]", typeItem.TypeNamespace), typeItem.Id, new TimeSpan(4, 0, 0), CacheItemPriority.NotRemovable);
                        core.Cache.SetCached(string.Format("itemApplicationIds[{0}]", typeItem.Id), typeItem.ApplicationId, new TimeSpan(4, 0, 0), CacheItemPriority.NotRemovable);

                        if (typeItem.IsPrimitive)
                        {
                            primitiveTypes.Add(typeItem.Id, typeItem.TypeNamespace);
                        }
                    }

                    core.Cache.SetCached("itemPrimitiveTypes", primitiveTypes, new TimeSpan(4, 0, 0), CacheItemPriority.High);

                    typesReader.Close();
                    typesReader.Dispose();
                }
            }

            return primitiveTypes;
        }

        private static ItemType updateItemTypeCache(Core core, string typeNamespace)
        {
            ItemType typeItem = null;

            SelectQuery query = ItemType.GetSelectQueryStub(core, typeof(ItemType));
            query.AddCondition("type_namespace", typeNamespace);

            System.Data.Common.DbDataReader typesReader = core.Db.ReaderQuery(query);

            if (typesReader.HasRows)
            {
                typesReader.Read();

                typeItem = new ItemType(core, typesReader);
                HibernateItem typeItemHibernate = new HibernateItem(typesReader);

                core.Cache.SetCached(string.Format("itemTypes[{0}]", typeItem.Id), typeItemHibernate, new TimeSpan(4, 0, 0), CacheItemPriority.NotRemovable);
                core.Cache.SetCached(string.Format("itemTypeIds[{0}]", typeItem.TypeNamespace), typeItem.Id, new TimeSpan(4, 0, 0), CacheItemPriority.NotRemovable);
                core.Cache.SetCached(string.Format("itemApplicationIds[{0}]", typeItem.Id), typeItem.ApplicationId, new TimeSpan(4, 0, 0), CacheItemPriority.NotRemovable);
            }

            typesReader.Close();
            typesReader.Dispose();

            return typeItem;
        }

        private static ItemType updateItemTypeCache(Core core, long typeId)
        {
            ItemType typeItem = null;

            SelectQuery query = ItemType.GetSelectQueryStub(core, typeof(ItemType));
            query.AddCondition("type_id", typeId);

            System.Data.Common.DbDataReader typesReader = core.Db.ReaderQuery(query);

            if (typesReader.HasRows)
            {
                typesReader.Read();

                typeItem = new ItemType(core, typesReader);
                HibernateItem typeItemHibernate = new HibernateItem(typesReader);

                core.Cache.SetCached(string.Format("itemTypes[{0}]", typeItem.Id), typeItemHibernate, new TimeSpan(4, 0, 0), CacheItemPriority.NotRemovable);
                core.Cache.SetCached(string.Format("itemTypeIds[{0}]", typeItem.TypeNamespace), typeItem.Id, new TimeSpan(4, 0, 0), CacheItemPriority.NotRemovable);
                core.Cache.SetCached(string.Format("itemApplicationIds[{0}]", typeItem.Id), typeItem.ApplicationId, new TimeSpan(4, 0, 0), CacheItemPriority.NotRemovable);
            }

            typesReader.Close();
            typesReader.Dispose();

            return typeItem;
        }

        public static long GetTypeId(Core core, Type type)
        {
            string typeNamespace = type.FullName;
            object o = core.Cache.GetCached(string.Format("itemTypeIds[{0}]", typeNamespace));

            if (o != null && o is long)
            {
                return (long)o;
            }
            else
            {
                ItemType itemType = updateItemTypeCache(core, typeNamespace);
                if (itemType != null)
                {
                    return itemType.Id;
                }
                else
                {
                    return 0;
                }
            }
        }

        public static ItemType GetItemType(Core core, long typeId)
        {
            object o = core.Cache.GetCached(string.Format("itemTypes[{0}]", typeId));

            if (o != null && o is HibernateItem)
            {
                return new ItemType(core, (HibernateItem)o);
            }
            else
            {
                return null;
            }
        }
        
        public override string ToString ()
        {
            return string.Format("Type: {0}, Id: {1}", TypeId, Id);
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
            return hashCode;
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
