﻿/*
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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public sealed class NumberedItemId : ItemKey, IComparable
    {
        private Type type;
        private int hashCode = 0;

        public NumberedItemId(long itemId, long typeId)
            : base(itemId, typeId)
        {
            this.type = null;
            hashCode = TypeId.GetHashCode() ^ Id.GetHashCode();
        }

        public new int CompareTo(object obj)
        {
            NumberedItemId pk = obj as NumberedItemId;
            if (pk == null) return -1;

            if (TypeId != pk.TypeId)
                return TypeId.CompareTo(pk.TypeId);
            return Id.CompareTo(pk.Id);
        }

        public override bool Equals(object obj)
        {
            NumberedItemId pk = obj as NumberedItemId;
            if (pk == null) return false;

            if (TypeId != pk.TypeId)
                return false;
            if (Id != pk.Id)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override string ToString()
        {
            if (TypeId > 0)
            {
                return string.Format("{0}.{1}",
                    TypeId, Id);
                ;
            }
            else
            {
                return string.Format("NULL.{0}",
                    Id);
                ;
            }
        }
    }

    public abstract class NumberedItem : Item
    {
        protected ItemKey key = null;
        internal ItemInfo info = null;

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

        [JsonProperty("key")]
        public ItemKey ItemKey
        {
            get
            {
                if (key == null)
                {
                    key = new ItemKey(Id, ItemType.GetTypeId(core, this.GetType()));
                }
                return key;
            }
        }

        [JsonProperty("info")]
        public ItemInfo Info
        {
            get
            {
                if (info == null)
                {
                    try
                    {
                        info = new ItemInfo(core, this);
                    }
                    catch (InvalidIteminfoException)
                    {
                        info = ItemInfo.Create(core, this);
                    }
                }
                return info;
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
            List<Item> items = new List<Item>(perPage);

            SelectQuery query;

#if DEBUG
            Stopwatch timer = new Stopwatch();
            timer.Start();
#endif
            bool dataReader = false;

            ConstructorInfo[] constructors = typeToGet.GetConstructors();

            // temporary
            foreach (ConstructorInfo constructor in constructors)
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                if (parameters.Length >= 2)
                {
                    if (parameters[1].ParameterType == typeof(System.Data.Common.DbDataReader))
                    {
                        dataReader = true;
                        break;
                    }
                }
            }
            // end temporary
#if DEBUG
            timer.Stop();
            if (HttpContext.Current != null)
            {
                //HttpContext.Current.Response.Write(string.Format("<!-- Constructor {1} found in {0} -->\r\n", timer.ElapsedTicks / 10000000.0, typeToGet.Name));
            }
#endif

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
                query = Item.GetSelectQueryStub(core, typeToGet);
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

            query.AddCondition(Item.GetTable(typeToGet) + "." + Item.GetParentField(core, this.GetType(), typeToGet), Id);

            if (!dataReader)
            {
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

                itemsTable.Dispose();
            }
            else
            {
                System.Data.Common.DbDataReader reader = core.Db.ReaderQuery(query);

                while (reader.Read())
                {
                    if (feedParentArgument)
                    {
                        items.Add(Activator.CreateInstance(typeToGet, new object[] { core, this, reader }) as Item);
                    }
                    else
                    {
                        items.Add(Activator.CreateInstance(typeToGet, new object[] { core, reader }) as Item);
                    }
                }

                reader.Close();
                reader.Dispose();
            }

            return items;
        }

        protected List<Tag> getTags()
        {
            List<Tag> tags = Tag.GetTags(core, this);

            return tags;
        }

        public override long Delete()
        {
            return Delete(false);
        }

        public override long Delete(bool parentDeleted)
        {
            ItemInfo info = this.Info;

            if (this is IPermissibleItem)
            {
                IPermissibleItem iThis = (IPermissibleItem)this;
                if (!iThis.Access.Can("DELETE"))
                {
                    throw new UnauthorisedToDeleteItemException();
                }
            }

            Type type = this.GetType();

            if (this is IPermissibleSubItem)
            {
                DeletePermissionAttribute[] deleteAttributes = (DeletePermissionAttribute[])this.GetType().GetCustomAttributes(typeof(DeletePermissionAttribute), false);

                if (deleteAttributes.Length == 1)
                {
                    if (string.IsNullOrEmpty(deleteAttributes[0].Key))
                    {
                        throw new UnauthorisedToUpdateItemException();
                    }
                    else
                    {
                        IPermissibleSubItem isThis = (IPermissibleSubItem)this;
                        if (!isThis.PermissiveParent.Access.Can(deleteAttributes[0].Key))
                        {
                            throw new UnauthorisedToUpdateItemException();
                        }
                    }
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

            return base.Delete(parentDeleted);
        }

        public static NumberedItem Reflect(Core core, ItemKey ik)
        {
            if (ik.GetType(core) != null)
            {
                if (ik.GetType(core).IsPrimitive)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ik);
                    return core.PrimitiveCache[ik];
                }
            }

            if (core.ItemCache.ContainsItem(ik))
            {
                return core.ItemCache[ik];
            }

            if (core == null)
            {
                throw new NullCoreException();
            }

            Type tType = null;

            if (ik.GetType(core).ApplicationId > 0)
            {
                core.ItemCache.RegisterType(typeof(ApplicationEntry));
                ItemKey applicationKey = new ItemKey(ik.GetType(core).ApplicationId, ItemKey.GetTypeId(core, typeof(ApplicationEntry)));
                core.ItemCache.RequestItem(applicationKey);
                ApplicationEntry ae = (ApplicationEntry)core.ItemCache[applicationKey];

                tType = ae.Assembly.GetType(ik.GetType(core).TypeNamespace);
            }
            else
            {
                tType = Type.GetType(ik.GetType(core).TypeNamespace);
            }

            return (Activator.CreateInstance(tType, new object[] { core, ik.Id }) as NumberedItem);
        }
    }
}
