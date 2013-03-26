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
    [DataTable("item_hashtags")]
    public class ItemHashtag : NumberedItem
    {
        [DataField("item_hashtag_id", DataFieldKeys.Primary)]
        private long itemTagId;
        [DataField("item_id")]
        private long itemId;
        [DataField("item_type_id")]
        private long itemTypeId;
        [DataField("hashtag_id")]
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

        public ItemHashtag(Core core, long itemTagId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemHashtag_ItemLoad);

            SelectQuery query = ItemHashtag_GetSelectQueryStub();
            query.AddCondition("item_hashtag_id", itemTagId);

            DataTable itemTagTable = db.Query(query);

            if (itemTagTable.Rows.Count == 1)
            {
                loadItemInfo(itemTagTable.Rows[0]);
                tag = new Tag(core, itemTagTable.Rows[0]);
            }
            else
            {
                throw new InvalidItemHashtagException();
            }
        }

        public ItemHashtag(Core core, DataRow itemHashtagRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemHashtag_ItemLoad);

            loadItemInfo(itemHashtagRow);
        }

        private void ItemHashtag_ItemLoad()
        {
        }

        public static SelectQuery ItemHashtag_GetSelectQueryStub()
        {
            SelectQuery query = NumberedItem.GetSelectQueryStub(typeof(ItemHashtag), false);

            query.AddFields(Tag.GetFieldsPrefixed(typeof(Tag)));
            query.AddJoin(JoinTypes.Inner, Tag.GetTable(typeof(Tag)), "hashtag_id", "hashtag_id");

            query.AddSort(SortOrder.Ascending, "tag_text_normalised");

            return query;
        }

        public static ItemHashtag Create(Core core, NumberedItem item, Tag tag)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            Item newItem = Item.Create(core, typeof(ItemHashtag), new FieldValuePair("item_id", item.Id),
                new FieldValuePair("item_type_id", item.ItemKey.TypeId),
                new FieldValuePair("hashtag_id", tag.Id));

            return (ItemHashtag)newItem;
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

    public class InvalidItemHashtagException : Exception
    {
    }

}
