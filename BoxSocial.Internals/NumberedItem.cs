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
    public abstract class NumberedItem : Item
    {
        protected NumberedItem(Core core)
            : base (core)
        {
        }

        public abstract long Id
        {
            get;
        }

        protected List<Item> getSubItems(Type typeToGet)
        {
            return getSubItems(typeToGet, 0, 0);
        }

        protected List<Item> getSubItems(Type typeToGet, int currentPage, int perPage)
        {
            return getSubItems(typeToGet, currentPage, perPage, false);
        }

        protected List<Item> getSubItems(Type typeToGet, int currentPage, int perPage, bool feedParentArgument)
        {
            List<Item> items = new List<Item>();

            SelectQuery query;

            if (typeToGet.GetMethod(typeToGet.Name + "_GetSelectQueryStub", new Type[] { typeof(Core) }) != null)
            {
                query = (SelectQuery)typeToGet.InvokeMember(typeToGet.Name + "_GetSelectQueryStub", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { core }); //GetSelectQueryStub(typeToGet);
            }
            else if (typeToGet.GetMethod(typeToGet.Name + "_GetSelectQueryStub", new Type[] { }) != null)
            {
                query = (SelectQuery)typeToGet.InvokeMember(typeToGet.Name + "_GetSelectQueryStub", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { }); //GetSelectQueryStub(typeToGet);
            }
            else
            {
                query = Item.GetSelectQueryStub(typeToGet);
            }

            if (perPage > 0)
            {
                query.LimitStart = (currentPage - 1) * perPage;
                query.LimitCount = perPage;
            }

            query.AddCondition(Item.GetTable(typeToGet) + "." + Item.GetParentField(this.GetType()), Id);

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

        public new long Delete()
        {
            if (this is IPermissibleItem)
            {
                if (!((IPermissibleItem)this).Access.CanDelete)
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
                    item.Delete();
                }
            }

            return base.Delete();
        }
    }
}
