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
    [DataTable("item_info")]
    public class ItemInfo : Item
    {
        [DataField("info_item", DataFieldKeys.Unique)]
        private ItemKey itemKey;
        [DataField("info_comments")]
        private long comments;
        [DataField("info_likes")]
        private long likes;
        [DataField("info_dislikes")]
        private long dislikes;
        [DataField("info_ratings")]
        private long ratings;
        [DataField("info_rating")]
        private float rating;
        [DataField("info_subscriptions")]
        private long subscriptions;
        [DataField("info_tags")]
        private long tags;
        [DataField("info_shared_times")]
        private long sharedTimes;
        [DataField("info_viewed_times")]
        private long viewedTimes;
        [DataField("info_item_time_ut")]
        private long timeRaw;

        private NumberedItem item;

        public ItemInfo(Core core, ItemKey itemKey)
            : this(core, null, itemKey.Id, itemKey.TypeId)
        {
        }

        public ItemInfo(Core core, long itemId, long itemTypeId)
            : this(core, null, itemId, itemTypeId)
        {
        }

        public ItemInfo(Core core, NumberedItem item)
            : this(core, item, item.ItemKey.Id, item.ItemKey.TypeId)
        {
        }

        public ItemInfo(Core core, NumberedItem item, long itemId, long itemTypeId)
            : base(core)
        {
            this.item = item;
            ItemLoad += new ItemLoadHandler(ItemInfo_ItemLoad);

            SelectQuery query = ItemInfo.GetSelectQueryStub(typeof(ItemInfo));
            query.AddCondition("info_item_id", itemId);
            query.AddCondition("info_item_type_id", itemTypeId);

            DataTable infoTable = db.Query(query);

            try
            {
                if (infoTable.Rows.Count == 1)
                {
                    loadItemInfo(infoTable.Rows[0]);
                }
                else
                {
                    throw new InvalidIteminfoException();
                }
            }
            catch (InvalidItemException)
            {
                throw new InvalidIteminfoException();
            }
        }

        public ItemInfo(Core core, DataRow itemRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemInfo_ItemLoad);

            try
            {
                loadItemInfo(itemRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidIteminfoException();
            }
        }

        private void ItemInfo_ItemLoad()
        {
        }

        public static ItemInfo Create(Core core, ItemKey itemKey)
        {
            return Create(core, null, itemKey);
        }

        public static ItemInfo Create(Core core, NumberedItem item)
        {
            return Create(core, item, item.ItemKey);
        }

        private static ItemInfo Create(Core core, NumberedItem item, ItemKey itemKey)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            /*Item.Create(core, typeof(ItemInfo), true,
                new FieldValuePair("info_item_id", itemKey.Id),
                new FieldValuePair("info_item_type_id", itemKey.TypeId),
                new FieldValuePair("info_item_time_ut", UnixTime.UnixTimeStamp()));*/

            InsertQuery iQuery = new InsertQuery(typeof(ItemInfo));
            iQuery.AddField("info_item_id", itemKey.Id);
            iQuery.AddField("info_item_type_id", itemKey.TypeId);
            iQuery.AddField("info_item_time_ut", UnixTime.UnixTimeStamp());
            
            core.Db.Query(iQuery);

            ItemInfo ii = new ItemInfo(core, item, itemKey.Id, itemKey.TypeId);

            return ii;
        }

        public long Comments
        {
            get
            {
                return comments;
            }
            internal set
            {
                SetPropertyByRef(new { comments }, value);
            }
        }

        public long Likes
        {
            get
            {
                return likes;
            }
            internal set
            {
                SetPropertyByRef(new { likes }, value);
            }
        }

        public long Dislikes
        {
            get
            {
                return dislikes;
            }
            internal set
            {
                SetPropertyByRef(new { dislikes }, value);
            }
        }

        public long Ratings
        {
            get
            {
                return ratings;
            }
            internal set
            {
                SetPropertyByRef(new { ratings }, value);
            }
        }

        public float Rating
        {
            get
            {
                return rating;
            }
            internal set
            {
                SetPropertyByRef(new { rating }, value);
            }
        }

        public long Subscriptions
        {
            get
            {
                return subscriptions;
            }
            internal set
            {
                SetPropertyByRef(new { subscriptions }, value);
            }
        }

        public long SharedTimes
        {
            get
            {
                return sharedTimes;
            }
            internal set
            {
                SetPropertyByRef(new { sharedTimes }, value);
            }
        }

        public long ViewedTimes
        {
            get
            {
                return viewedTimes;
            }
            internal set
            {
                SetPropertyByRef(new { viewedTimes }, value);
            }
        }

        internal void IncrementLikes()
        {
            AdjustLikes(1);
        }

        internal void DecrementLikes()
        {
            AdjustLikes(-1);
        }

        internal void AdjustLikes(int by)
        {
            UpdateQuery uQuery = new UpdateQuery(typeof(ItemInfo));
            uQuery.AddCondition("info_item_id", itemKey.Id);
            uQuery.AddCondition("info_item_type_id", itemKey.TypeId);
            uQuery.AddField("info_likes", new QueryOperation("info_likes", QueryOperations.Addition, by));

            core.Db.Query(uQuery);
        }

        internal void IncrementDislikes()
        {
            AdjustDislikes(1);
        }

        internal void DecrementDislikes()
        {
            AdjustDislikes(-1);
        }

        internal void AdjustDislikes(int by)
        {
            UpdateQuery uQuery = new UpdateQuery(typeof(ItemInfo));
            uQuery.AddCondition("info_item_id", itemKey.Id);
            uQuery.AddCondition("info_item_type_id", itemKey.TypeId);
            uQuery.AddField("info_dislikes", new QueryOperation("info_dislikes", QueryOperations.Addition, by));

            core.Db.Query(uQuery);
        }

        internal void IncrementComments()
        {
            AdjustComments(1);
        }

        internal void DecrementComments()
        {
            AdjustComments(-1);
        }

        internal void AdjustComments(int by)
        {
            UpdateQuery uQuery = new UpdateQuery(typeof(ItemInfo));
            uQuery.AddCondition("info_item_id", itemKey.Id);
            uQuery.AddCondition("info_item_type_id", itemKey.TypeId);
            uQuery.AddField("info_comments", new QueryOperation("info_comments", QueryOperations.Addition, by));

            core.Db.Query(uQuery);
        }

        internal void IncrementRatings()
        {
            AdjustRatings(1);
        }

        internal void DecrementRatings()
        {
            AdjustRatings(-1);
        }

        internal void AdjustRatings(int by)
        {
            UpdateQuery uQuery = new UpdateQuery(typeof(ItemInfo));
            uQuery.AddCondition("info_item_id", itemKey.Id);
            uQuery.AddCondition("info_item_type_id", itemKey.TypeId);
            uQuery.AddField("info_ratings", new QueryOperation("info_ratings", QueryOperations.Addition, by));

            core.Db.Query(uQuery);
        }

        internal void IncrementSubscriptions()
        {
            AdjustSubscriptions(1);
        }

        internal void DecrementSubscriptions()
        {
            AdjustSubscriptions(-1);
        }

        internal void AdjustSubscriptions(int by)
        {
            UpdateQuery uQuery = new UpdateQuery(typeof(ItemInfo));
            uQuery.AddCondition("info_item_id", itemKey.Id);
            uQuery.AddCondition("info_item_type_id", itemKey.TypeId);
            uQuery.AddField("info_subscriptions", new QueryOperation("info_subscriptions", QueryOperations.Addition, by));

            core.Db.Query(uQuery);
        }

        internal void IncrementSharedTimes()
        {
            AdjustSharedTimes(1);
        }

        internal void DecrementSharedTimes()
        {
            AdjustSharedTimes(-1);
        }

        internal void AdjustSharedTimes(int by)
        {
            UpdateQuery uQuery = new UpdateQuery(typeof(ItemInfo));
            uQuery.AddCondition("info_item_id", itemKey.Id);
            uQuery.AddCondition("info_item_type_id", itemKey.TypeId);
            uQuery.AddField("info_shared_times", new QueryOperation("info_shared_times", QueryOperations.Addition, by));

            core.Db.Query(uQuery);
        }

        internal void IncrementViewedTimes()
        {
            AdjustViewedTimes(1);
        }

        internal void DecrementViewedTimes()
        {
            AdjustViewedTimes(-1);
        }

        internal void AdjustViewedTimes(int by)
        {
            UpdateQuery uQuery = new UpdateQuery(typeof(ItemInfo));
            uQuery.AddCondition("info_item_id", itemKey.Id);
            uQuery.AddCondition("info_item_type_id", itemKey.TypeId);
            uQuery.AddField("info_viewed_times", new QueryOperation("info_viewed_times", QueryOperations.Addition, by));

            core.Db.Query(uQuery);
        }

        private ItemKey ItemKey
        {
            get
            {
                return itemKey;
            }
        }

        private NumberedItem Item
        {
            get
            {
                if (item == null || item.ItemKey != ItemKey)
                {
                    item = NumberedItem.Reflect(core, ItemKey);
                }
                return item;
            }
        }

        public override string Uri
        {
            get
            {
                if (Item != null)
                {
                    return Item.Uri;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }

    public class InvalidIteminfoException : Exception
    {
    }
}
