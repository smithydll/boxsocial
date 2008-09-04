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
        [DataField("tab_item_id")]
        private long ownerId;
        [DataField("tab_item_type", 15)]
        private string ownerType;
        [DataField("tab_order")]
        private byte order;

        private Primitive owner;
        private string tabTitle;

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

        public string Title
        {
            get
            {
                return tabTitle;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerId != owner.Id || ownerType != owner.Type)
                {
                    core.UserProfiles.LoadPrimitiveProfile(ownerType, ownerId);
                    owner = core.UserProfiles[ownerType, ownerId];
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
                tabTitle = (string)tabRow["page_title"];
            }
            catch (InvalidItemException)
            {
                throw new InvalidNavigationTabException();
            }
        }

        void NagivationTab_ItemLoad()
        {
        }

        public static SelectQuery NagivationTab_GetSelectQueryStub()
        {
            SelectQuery query = NagivationTab.GetSelectQueryStub(typeof(NagivationTab), false);
            query.AddFields(string.Format("`{0}`.`page_title`", Page.GetTable(typeof(Page))));
            query.AddJoin(JoinTypes.Left, ContentLicense.GetTable(typeof(Page)), "tab_page_id", "page_id");

            return query;
        }

        public static NagivationTab Create(Core core, Page page)
        {
            byte order = 0;

            SelectQuery query = NagivationTab.GetSelectQueryStub(typeof(NagivationTab));
            query.AddCondition("tab_item_id", page.Owner.Id);
            query.AddCondition("tab_item_type", page.Owner.Type);
            query.AddSort(SortOrder.Descending, "tab_order");
            query.LimitCount = 1;

            DataTable tabOrderTable = core.db.Query(query);

            if (tabOrderTable.Rows.Count == 1)
            {
                NagivationTab tab = new NagivationTab(core, tabOrderTable.Rows[0]);

                order = (byte)(tab.Order + 1);
            }

            InsertQuery iQuery = new InsertQuery(NagivationTab.GetTable(typeof(NagivationTab)));
            iQuery.AddField("tab_page_id", page.Id);
            iQuery.AddField("tab_item_id", page.Owner.Id);
            iQuery.AddField("tab_item_type", page.Owner.Type);
            iQuery.AddField("tab_order", order);

            long tabId = core.db.Query(iQuery);

            return new NagivationTab(core, tabId);
        }

        public static List<NagivationTab> GetTabs(Core core, Primitive owner)
        {
            List<NagivationTab> tabs = new List<NagivationTab>();

            SelectQuery query = NagivationTab.GetSelectQueryStub(typeof(NagivationTab));
            query.AddCondition("tab_item_id", owner.Id);
            query.AddCondition("tab_item_type", owner.Type);
            query.AddSort(SortOrder.Ascending, "tab_order");

            DataTable tabTable = core.db.Query(query);

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

        public override string Namespace
        {
            get
            {
                return this.GetType().FullName;
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
