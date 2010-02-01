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

            SelectQuery query = ItemTag_GetSelectQueryStub();
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

        private void ItemTag_ItemLoad()
        {
        }

        public static SelectQuery ItemTag_GetSelectQueryStub()
        {
            SelectQuery query = NumberedItem.GetSelectQueryStub(typeof(ItemTag), false);

            query.AddFields(Tag.GetFieldsPrefixed(typeof(Tag)));
            query.AddJoin(JoinTypes.Inner, Tag.GetTable(typeof(Tag)), "tag_id", "tag_id");

            query.AddSort(SortOrder.Ascending, "tag_text_normalised");

            return query;
        }

        public static ItemTag Create(Core core, NumberedItem item, Tag tag)
        {
            Item newItem = Item.Create(core, typeof(ItemTag), new FieldValuePair("item_id", item.Id),
                new FieldValuePair("item_type_id", item.ItemKey.TypeId),
                new FieldValuePair("tag_id", tag.Id));

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
