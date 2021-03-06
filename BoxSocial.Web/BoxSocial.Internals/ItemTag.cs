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
using System.Text;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("item_tags")]
    public class ItemTag : NumberedItem
    {
        [DataField("item_tag_id", DataFieldKeys.Primary)]
        private long itemTagId;
        [DataField("item_id")]
        private long itemId;
        [DataField("item_type_id")]
        private long itemTypeId;
        [DataField("tag_id")]
        private long tagId;

        private Tag tag;

        public long ItemTagId
        {
            get
            {
                return itemTagId;
            }
        }

        public long ItemId
        {
            get
            {
                return itemId;
            }
        }

        public long ItemTypeId
        {
            get
            {
                return itemTypeId;
            }
        }

        public long TagId
        {
            get
            {
                return tagId;
            }
        }

        public ItemTag(Core core, long itemTagId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemTag_ItemLoad);

            SelectQuery query = ItemTag_GetSelectQueryStub(core);
            query.AddCondition("item_tag_id", itemTagId);

            DataTable itemTagTable = db.Query(query);

            if (itemTagTable.Rows.Count == 1)
            {
                loadItemInfo(itemTagTable.Rows[0]);
                tag = new Tag(core, itemTagTable.Rows[0]);
            }
            else
            {
                throw new InvalidItemTagException();
            }
        }

        public ItemTag(Core core, DataRow itemTagRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemTag_ItemLoad);

            loadItemInfo(itemTagRow);
        }

        public ItemTag(Core core, System.Data.Common.DbDataReader itemTagRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemTag_ItemLoad);

            loadItemInfo(itemTagRow);
        }

        protected override void loadItemInfo(DataRow itemTagRow)
        {
            loadValue(itemTagRow, "item_tag_id", out itemTagId);
            loadValue(itemTagRow, "item_id", out itemId);
            loadValue(itemTagRow, "item_type_id", out itemTypeId);
            loadValue(itemTagRow, "tag_id", out tagId);

            itemLoaded(itemTagRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader itemTagRow)
        {
            loadValue(itemTagRow, "item_tag_id", out itemTagId);
            loadValue(itemTagRow, "item_id", out itemId);
            loadValue(itemTagRow, "item_type_id", out itemTypeId);
            loadValue(itemTagRow, "tag_id", out tagId);

            itemLoaded(itemTagRow);
            core.ItemCache.RegisterItem((NumberedItem)this);
        }

        private void ItemTag_ItemLoad()
        {
        }

        public static SelectQuery ItemTag_GetSelectQueryStub(Core core)
        {
            long typeId = ItemType.GetTypeId(core, typeof(ItemTag));
            /*if (QueryCache.HasQuery(typeId))
            {
                return (SelectQuery)QueryCache.GetQuery(typeof(ItemTag), typeId);
            }
            else*/
            {
                SelectQuery query = NumberedItem.GetSelectQueryStub(core, typeof(ItemTag), false);

                query.AddFields(Tag.GetFieldsPrefixed(core, typeof(Tag)));
                query.AddJoin(JoinTypes.Inner, Tag.GetTable(typeof(Tag)), "tag_id", "tag_id");

                return query;
            }
        }

        public static ItemTag Create(Core core, NumberedItem item, Tag tag)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Item newItem = Item.Create(core, typeof(ItemTag), new FieldValuePair("item_id", item.Id),
                new FieldValuePair("item_type_id", item.ItemKey.TypeId),
                new FieldValuePair("tag_id", tag.Id));

            return (ItemTag)newItem;
        }

        public static ItemTag Create(Core core, NumberedItem item, long tagId)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Item newItem = Item.Create(core, typeof(ItemTag), new FieldValuePair("item_id", item.Id),
                new FieldValuePair("item_type_id", item.ItemKey.TypeId),
                new FieldValuePair("tag_id", tagId));

            return (ItemTag)newItem;
        }

        public override long Id
        {
            get
            {
                return itemTagId;
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

    public class InvalidItemTagException : Exception
    {
    }
}
