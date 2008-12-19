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
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("actions")]
    public class Action : NumberedItem
    {
        [DataField("action_id", DataFieldKeys.Primary)]
        private long actionId;
        [DataField("action_title", 63)]
        private string title;
        [DataField("action_body", 511)]
        private string body;
        [DataField("action_application")]
        private long applicationId;
        [DataField("action_primitive_id")]
        private long primitiveId;
        [DataField("action_primitive_type", 15)]
        private string primitiveType;
        [DataField("action_time_ut")]
        private long timeRaw;

        private Primitive owner;

        public long ActionId
        {
            get
            {
                return actionId;
            }
        }

        public string Title
        {
            get
            {
                if (title.Contains("[/user]"))
                {
                    return title;
                }
                else
                {
                    return string.Format("[user]{0}[/user] {1}",
                        primitiveId, title);
                }
            }
        }

        public string Body
        {
            get
            {
                return body;
            }
        }

        public long OwnerId
        {
            get
            {
                return primitiveId;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || primitiveId != owner.Id || primitiveType != owner.Type)
                {
                    core.UserProfiles.LoadPrimitiveProfile(primitiveType, primitiveId);
                    owner = core.UserProfiles[primitiveType, primitiveId];
                    return owner;
                }
                else
                {
                    return owner;
                }
            }
        }

        public Action(Core core, Primitive owner, DataRow actionRow)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(Action_ItemLoad);

            loadItemInfo(actionRow);
        }

        private void Action_ItemLoad()
        {
        }

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timeRaw);
        }

        public List<ActionItem> GetActionItems()
        {
            return getSubItems(typeof(ActionItem)).ConvertAll<ActionItem>(new Converter<Item, ActionItem>(convertToActionItem));
        }

        public static List<Action> GetActions(Core core, User user, long itemId, string itemType)
        {
            List<Action> actions = new List<Action>();

            SelectQuery query = Action.GetSelectQueryStub(typeof(Action));
            query.AddJoin(JoinTypes.Inner, ActionItem.GetTable(typeof(ActionItem)), "action_id", "action_id");
            query.AddCondition("user_id", user.Id);
            query.AddCondition("item_id", itemId);
            query.AddCondition("item_type", itemType);
            query.AddSort(SortOrder.Descending, "status_time_ut");

            DataTable actionsTable = core.db.Query(query);

            foreach (DataRow dr in actionsTable.Rows)
            {
                actions.Add(new Action(core, user, dr));
            }

            return actions;
        }

        public DataTable GetItemsData(Type type)
        {
            SelectQuery query = Item.GetSelectQueryStub(typeof(ActionItem));
            query.AddFields(Item.GetFieldsPrefixed(type));
            query.AddCondition("item_type", Item.GetNamespace(type));
            query.AddJoin(JoinTypes.Inner, Item.GetTable(type), "item_id", "gallery_item_id");

            DataTable itemsTable = db.Query(query);

            return itemsTable;
        }

        public ActionItem convertToActionItem(Item input)
        {
            return (ActionItem)input;
        }

        public override long Id
        {
            get
            {
                return actionId;
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
}
