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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Applications.Pages
{
    [DataTable("navigation_tabs")]
    public class NagivationTab : NumberedItem
    {
        [DataField("tab_id", DataFieldKeys.Primary)]
        private long tabId;
        [DataField("tab_page_id", typeof(Page))]
        private long pageId;
        /*[DataField("tab_item_id")]
        private long ownerId;
        [DataField("tab_item_type", 15)]
        private string ownerType;*/
        [DataField("tab_item", DataFieldKeys.Index)]
        private ItemKey ownerKey;
        [DataField("tab_order")]
        private byte order;

        private Primitive owner;
        private Page page;

        public long TabId
        {
            get
            {
                return tabId;
            }
        }

        public long PageId
        {
            get
            {
                return pageId;
            }
        }

        public byte Order
        {
            get
            {
                return order;
            }
        }

        public Page Page
        {
            get
            {
                return page;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.ItemKey.Id || ownerKey.TypeId != owner.ItemKey.TypeId)
                {
                    core.PrimitiveCache.LoadPrimitiveProfile(ownerKey);
                    owner = core.PrimitiveCache[ownerKey];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public NagivationTab(Core core, long tabId)
            : base (core)
        {
            ItemLoad += new ItemLoadHandler(NagivationTab_ItemLoad);

            try
            {
                LoadItem(tabId);
            }
            catch (InvalidItemException)
            {
                throw new InvalidNavigationTabException();
            }
        }

        public NagivationTab(Core core, DataRow tabRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(NagivationTab_ItemLoad);

            try
            {
                loadItemInfo(tabRow);
                try
                {
                    page = new Page(core, Owner, tabRow);
                }
                catch
                {
                    // ignore
                }
            }
            catch (InvalidItemException)
            {
                throw new InvalidNavigationTabException();
            }
        }

        void NagivationTab_ItemLoad()
        {
        }

        public void MoveUp()
        {
            this.AuthenticateAction(ItemChangeAction.Edit);

            if (order != 0)
            {
                UpdateQuery uQuery = new UpdateQuery(Item.GetTable(typeof(NagivationTab)));
                uQuery.AddField("tab_order", new QueryOperation("tab_order", QueryOperations.Addition, 1));
                uQuery.AddCondition("tab_item_id", ownerKey.Id);
                uQuery.AddCondition("tab_item_type_id", ownerKey.TypeId);
                uQuery.AddCondition("tab_order", ConditionEquality.Equal, order - 1);

                db.Query(uQuery);

                SetProperty("order", (byte)(order - 1));

                Update();
            }
        }

        public void MoveDown()
        {
            this.AuthenticateAction(ItemChangeAction.Edit);

            byte maxOrder = 0;

            SelectQuery query = GetSelectQueryStub(typeof(NagivationTab), false);
            query.AddCondition("tab_item_id", ownerKey.Id);
            query.AddCondition("tab_item_type_id", ownerKey.TypeId);
            query.AddSort(SortOrder.Descending, "tab_order");
            query.LimitCount = 1;

            DataTable tabTable = db.Query(query);

            if (tabTable.Rows.Count == 1)
            {
                maxOrder = (byte)tabTable.Rows[0]["tab_order"];
            }

            if (order < maxOrder)
            {
                UpdateQuery uQuery = new UpdateQuery(Item.GetTable(typeof(NagivationTab)));
                uQuery.AddField("tab_order", new QueryOperation("tab_order", QueryOperations.Subtraction, 1));
                uQuery.AddCondition("tab_item_id", ownerKey.Id);
                uQuery.AddCondition("tab_item_type_id", ownerKey.TypeId);
                uQuery.AddCondition("tab_order", ConditionEquality.Equal, order + 1);

                db.Query(uQuery);

                SetProperty("order", (byte)(order + 1));

                Update();
            }
        }

        public static SelectQuery NagivationTab_GetSelectQueryStub()
        {
            SelectQuery query = NagivationTab.GetSelectQueryStub(typeof(NagivationTab), false);
            query.AddFields(Page.GetFieldsPrefixed(typeof(Page)));
            query.AddJoin(JoinTypes.Left, ContentLicense.GetTable(typeof(Page)), "tab_page_id", "page_id");

            return query;
        }

        public static NagivationTab Create(Core core, Page page)
        {
            byte order = 0;

            SelectQuery query = NagivationTab.GetSelectQueryStub(typeof(NagivationTab));
            query.AddCondition("tab_item_id", page.Owner.Id);
            query.AddCondition("tab_item_type_id", page.Owner.TypeId);
            query.AddSort(SortOrder.Descending, "tab_order");
            query.LimitCount = 1;

            DataTable tabOrderTable = core.Db.Query(query);

            if (tabOrderTable.Rows.Count == 1)
            {
                NagivationTab tab = new NagivationTab(core, tabOrderTable.Rows[0]);

                order = (byte)(tab.Order + 1);
            }

            InsertQuery iQuery = new InsertQuery(NagivationTab.GetTable(typeof(NagivationTab)));
            iQuery.AddField("tab_page_id", page.Id);
            iQuery.AddField("tab_item_id", page.Owner.Id);
            iQuery.AddField("tab_item_type_id", page.Owner.TypeId);
            iQuery.AddField("tab_order", order);

            long tabId = core.Db.Query(iQuery);

            return new NagivationTab(core, tabId);
        }

        public static List<NagivationTab> GetTabs(Core core, Primitive owner)
        {
            List<NagivationTab> tabs = new List<NagivationTab>();

            SelectQuery query = NagivationTab.GetSelectQueryStub(typeof(NagivationTab));
            query.AddCondition("tab_item_id", owner.Id);
            query.AddCondition("tab_item_type_id", owner.TypeId);
            query.AddSort(SortOrder.Ascending, "tab_order");

            DataTable tabTable = core.Db.Query(query);

            foreach (DataRow dr in tabTable.Rows)
            {
                tabs.Add(new NagivationTab(core, dr));
            }
            
            return tabs;
        }

        public override long Id
        {
            get
            {
                return tabId;
            }
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class InvalidNavigationTabException : Exception
    {
    }
}
