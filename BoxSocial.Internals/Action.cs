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
		[DataField("action_primitive", DataFieldKeys.Index)]
        private ItemKey ownerKey;
        [DataField("action_item", DataFieldKeys.Index)]
        private ItemKey itemKey;
        [DataField("action_time_ut")]
        private long timeRaw;

        private Primitive owner;

        public new ItemInfo Info
        {
            get
            {
                if (info == null)
                {
                    try
                    {
                        info = new ItemInfo(core, ActionItemKey);
                    }
                    catch (InvalidIteminfoException)
                    {
                        info = ItemInfo.Create(core, ActionItemKey);
                    }
                }
                return info;
            }
        }

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
                        ownerKey.Id, title);
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
                return ownerKey.Id;
            }
        }

        public ItemKey ActionItemKey
        {
            get
            {
                return itemKey;
            }
        }

        public Primitive Owner
        {
            get
            {
                if (owner == null || ownerKey.Id != owner.Id || ownerKey.TypeId != owner.TypeId)
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

        public Action(Core core, Primitive owner, DataRow actionRow)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(Action_ItemLoad);

            loadItemInfo(actionRow);

            try
            {
                this.info = new ItemInfo(core, actionRow);
            }
            catch (InvalidIteminfoException)
            {
                // not all rows will have one yet, but be ready
            }
            catch //(Exception ex)
            {
                //HttpContext.Current.Response.Write(ex.ToString());
                //HttpContext.Current.Response.End();
                // catch all remaining errors
            }
        }

        private void Action_ItemLoad()
        {
        }

        public static SelectQuery Action_GetSelectQueryStub()
        {
            SelectQuery query = Action.GetSelectQueryStub(typeof(Action), false);
            query.AddFields(ItemInfo.GetFieldsPrefixed(typeof(ItemInfo)));
            TableJoin join = query.AddJoin(JoinTypes.Left, new DataField(typeof(Action), "action_item_id"), new DataField(typeof(ItemInfo), "info_item_id"));
            join.AddCondition(new DataField(typeof(Action), "action_item_type_id"), new DataField(typeof(ItemInfo), "info_item_type_id"));

            return query;
        }

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timeRaw);
        }

        public List<ActionItem> GetActionItems()
        {
            return getSubItems(typeof(ActionItem)).ConvertAll<ActionItem>(new Converter<Item, ActionItem>(convertToActionItem));
        }

        public static List<Action> GetActions(Core core, User user, long itemId, long itemTypeId)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            List<Action> actions = new List<Action>();

            SelectQuery query = Action.GetSelectQueryStub(typeof(Action));
            query.AddJoin(JoinTypes.Inner, ActionItem.GetTable(typeof(ActionItem)), "action_id", "action_id");
            query.AddCondition("user_id", user.Id);
            query.AddCondition("item_id", itemId);
            query.AddCondition("item_type_id", itemTypeId);
            query.AddSort(SortOrder.Descending, "status_time_ut");

            DataTable actionsTable = core.Db.Query(query);

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
            query.AddCondition("item_type_id", ItemKey.GetTypeId(type));
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
