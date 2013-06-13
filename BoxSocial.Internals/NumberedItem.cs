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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    public sealed class NumberedItemId : ItemKey, IComparable
    {
        private Type type;

        public NumberedItemId(long itemId, long typeId)
            : base(itemId, typeId)
        {
            this.type = null;
        }

        public NumberedItemId(long itemId, Type type)
            : base(itemId, type)
        {
            this.type = type;
        }

        public int CompareTo(object obj)
        {
            if (obj.GetType() != typeof(NumberedItemId)) return -1;
            NumberedItemId pk = (NumberedItemId)obj;

            if (TypeId != pk.TypeId)
                return TypeId.CompareTo(pk.TypeId);
            return Id.CompareTo(pk.Id);
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(NumberedItemId)) return false;
            NumberedItemId pk = (NumberedItemId)obj;

            if (TypeId != pk.TypeId)
                return false;
            if (Id != pk.Id)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return TypeId.GetHashCode() ^ Id.GetHashCode();
        }

        public override string ToString()
        {
            if (TypeId < 0)
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

        protected List<Tag> getTags()
        {
            List<Tag> tags = Tag.GetTags(core, this);

            return tags;
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

            return base.Delete();
        }

        public static NumberedItem Reflect(Core core, ItemKey ik)
        {
            if (ik.Type != null)
            {
                if (ik.Type.IsSubclassOf(typeof(Primitive)))
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
            
            if (ik.ApplicationId > 0)
            {
                core.ItemCache.RegisterType(typeof(ApplicationEntry));
                ItemKey applicationKey = new ItemKey(ik.ApplicationId, typeof(ApplicationEntry));
                core.ItemCache.RequestItem(applicationKey);
                //ApplicationEntry ae = new ApplicationEntry(core, ik.ApplicationId);
                ApplicationEntry ae = (ApplicationEntry)core.ItemCache[applicationKey];
    
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

                tType = assembly.GetType(ik.TypeString);
            }
            else
            {
                tType = Type.GetType(ik.TypeString);
            }

            return (Activator.CreateInstance(tType, new object[] { core, ik.Id }) as NumberedItem);
        }
    }
}
