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
using System.Web;
using BoxSocial.Internals;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("action_items")]
    public class ActionItem : Item
    {
        [DataField("action_id", typeof(Action), DataFieldKeys.Unique, "ternary")]
        private long actionId;
        [DataField("item_id", DataFieldKeys.Unique, "ternary")]
        private long itemId;
        [DataField("item_type", DataFieldKeys.Unique, "ternary", 15)]
        private string itemType;

        public long ActionId
        {
            get
            {
                return actionId;
            }
        }

        public long ItemId
        {
            get
            {
                return itemId;
            }
        }

        public string ItemType
        {
            get
            {
                return itemType;
            }
        }

        public ActionItem(Core core, long actionId, long itemId, string itemType)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ActionItem_ItemLoad);

            SelectQuery query = ActionItem.GetSelectQueryStub(typeof(ActionItem));
            query.AddCondition("action_id", actionId);
            query.AddCondition("item_id", itemId);
            query.AddCondition("item_type", itemType);

            DataTable actionItemTable = db.Query(query);

            if (actionItemTable.Rows.Count == 1)
            {
                loadItemInfo(actionItemTable.Rows[0]);
            }
            else
            {
                throw new InvalidActionItemException();
            }
        }

        private void ActionItem_ItemLoad()
        {
        }

        public static void Create(Core core, long actionId, long itemId, string itemType)
        {
            InsertQuery iquery = new InsertQuery(ActionItem.GetTable(typeof(ActionItem)));
            iquery.AddField("action_id", actionId);
            iquery.AddField("item_id", itemId);
            iquery.AddField("item_type", itemType);

            core.db.Query(iquery);
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

    public class InvalidActionItemException : Exception
    {
    }
}
