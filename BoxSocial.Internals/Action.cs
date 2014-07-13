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
    public class Action : NumberedItem, IPermissibleSubItem
    {
        [DataFieldKey(DataFieldKeys.Index, "i_sort")]
        [DataField("action_id", DataFieldKeys.Primary)]
        private long actionId;
        [DataField("action_title", 127)]
        private string title;
        [DataField("action_body", 511)]
        private string body;
        [DataField("action_body_cache", 511)]
        private string bodyCache;
        [DataField("action_application")]
        private long applicationId;
		[DataField("action_primitive", DataFieldKeys.Index)]
        private ItemKey ownerKey;
        [DataField("action_item", DataFieldKeys.Index)]
        private ItemKey itemKey;
        [DataField("interact_item", DataFieldKeys.Index)]
        private ItemKey interactKey;
        [DataFieldKey(DataFieldKeys.Index, "i_sort")]
        [DataField("action_time_ut")]
        private long timeRaw;

        private Primitive owner;
        private IActionableItem item;
        private NumberedItem interactItem;

        public new ItemInfo Info
        {
            get
            {
                if (InteractItemKey.TypeId > 0)
                {
                    if (info == null || info.InfoKey.Id != InteractItemKey.Id || info.InfoKey.TypeId != InteractItemKey.TypeId)
                    {
                        try
                        {
                            info = new ItemInfo(core, InteractItemKey);
                        }
                        catch (InvalidIteminfoException)
                        {
                            info = ItemInfo.Create(core, InteractItemKey);
                        }
                    }
                }
                else
                {
                    if (info == null || info.InfoKey.Id != InteractItemKey.Id || info.InfoKey.TypeId != InteractItemKey.TypeId)
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
                }
                return info;
            }
        }

        public ItemKey ActionItemKey
        {
            get
            {
                return itemKey;
            }
        }

        public ItemKey InteractItemKey
        {
            get
            {
                return interactKey;
            }
        }

        public IActionableItem ActionedItem
        {
            get
            {
                if (item == null)
                {
                    core.ItemCache.RequestItem(itemKey);
                    try
                    {
                        item = (IActionableItem)core.ItemCache[itemKey];
                    }
                    catch
                    {
                        try
                        {
                            item = (IActionableItem)NumberedItem.Reflect(core, itemKey);
                            HttpContext.Current.Response.Write("<br />Fallback, had to reflect: " + itemKey.ToString());
                        }
                        catch
                        {
                            item = null;
                        }
                    }
                }
                return item;
            }
        }

        public NumberedItem InteractItem
        {
            get
            {
                if (interactKey == itemKey)
                {
                    return (NumberedItem)ActionedItem;
                }
                if (interactItem == null)
                {
                    core.ItemCache.RequestItem(interactKey);
                    try
                    {
                        interactItem = (NumberedItem)core.ItemCache[interactKey];
                    }
                    catch
                    {
                        try
                        {
                            interactItem = NumberedItem.Reflect(core, interactKey);
                            HttpContext.Current.Response.Write("<br />Fallback, had to reflect: " + interactKey.ToString());
                        }
                        catch
                        {
                            interactItem = null;
                        }
                    }
                }
                return interactItem;
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
                string url = string.Empty;
                if (ActionedItem != null)
                {
                    title = ActionedItem.Action;
                    url = ActionedItem.Uri;
                }
                return GetTitle(ownerKey, title, core.Hyperlink.StripSid(url));
            }
        }

        public static string GetTitle(ItemKey ownerKey, string title, string url)
        {
            if (title.Contains("[/user]"))
            {
                return title;
            }
            else
            {
                if (url != string.Empty)
                {
                    return string.Format("[user]{0}[/user] [iurl=\"{2}\"]{1}[/iurl]",
                        ownerKey.Id, title, url);
                }
                else
                {
                    return string.Format("[user]{0}[/user] {1}",
                    ownerKey.Id, title);
                }
            }
        }

        public static string GetTitle(string ownerDisplayName, string title)
        {
            return string.Format("{0} {1}", ownerDisplayName, title);
        }

        public string Body
        {
            get
            {
                return body;
            }
        }

        public string BodyCache
        {
            get
            {
                return bodyCache;
            }
        }

        public long OwnerId
        {
            get
            {
                return ownerKey.Id;
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
                }
                return owner;
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

        public Action(Core core, Primitive owner, System.Data.Common.DbDataReader actionReader)
            : base(core)
        {
            this.owner = owner;
            ItemLoad += new ItemLoadHandler(Action_ItemLoad);

            loadItemInfo(actionReader);

            try
            {
                this.info = new ItemInfo(core, actionReader);
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
            TableJoin join = query.AddJoin(JoinTypes.Left, new DataField(typeof(Action), "interact_item_id"), new DataField(typeof(ItemInfo), "info_item_id"));
            join.AddCondition(new DataField(typeof(Action), "interact_item_type_id"), new DataField(typeof(ItemInfo), "info_item_type_id"));

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


        public IPermissibleItem PermissiveParent
        {
            get
            {
                if (ActionedItem is IPermissibleItem)
                {
                    return (IPermissibleItem)ActionedItem;
                }
                else if (ActionedItem is IPermissibleSubItem)
                {
                    return ((IPermissibleSubItem)ActionedItem).PermissiveParent;
                }
                else
                {
                    return Owner;
                }
            }
        }
    }
}
