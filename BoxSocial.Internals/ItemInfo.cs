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
    [DataTable("item_info")]
    public class ItemInfo : Item
    {
        [DataField("item", DataFieldKeys.Index)]
        private ItemKey itemKey;
        [DataField("comments")]
        private long comments;
        [DataField("likes")]
        private long likes;
        [DataField("dislikes")]
        private long dislikes;
        [DataField("ratings")]
        private long ratings;
        [DataField("subscriptions")]
        private long subscriptions;
        [DataField("tags")]
        private long tags;
        [DataField("item_time_ut")]
        private long timeRaw;

        public ItemInfo(Core core, long itemId, long itemTypeId)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemInfo_ItemLoad);

            try
            {
                LoadItem("item", new ItemKey(itemId, itemTypeId));
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

            loadItemInfo(itemRow);
        }

        private void ItemInfo_ItemLoad()
        {
        }

        public long Comments
        {
            get
            {
                return comments;
            }
            internal set
            {
                SetProperty("comments", value);
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
                SetProperty("likes", value);
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
                SetProperty("dislikes", value);
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
                SetProperty("ratings", value);
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
                SetProperty("subscriptions", value);
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
            uQuery.AddCondition("item_id", itemKey.Id);
            uQuery.AddCondition("item_type_id", itemKey.TypeId);
            uQuery.AddField("likes", new QueryOperation("likes", QueryOperations.Addition, by));

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
            uQuery.AddCondition("item_id", itemKey.Id);
            uQuery.AddCondition("item_type_id", itemKey.TypeId);
            uQuery.AddField("dislikes", new QueryOperation("dislikes", QueryOperations.Addition, by));

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
            uQuery.AddCondition("item_id", itemKey.Id);
            uQuery.AddCondition("item_type_id", itemKey.TypeId);
            uQuery.AddField("comments", new QueryOperation("comments", QueryOperations.Addition, by));

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
            uQuery.AddCondition("item_id", itemKey.Id);
            uQuery.AddCondition("item_type_id", itemKey.TypeId);
            uQuery.AddField("ratings", new QueryOperation("ratings", QueryOperations.Addition, by));

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
            uQuery.AddCondition("item_id", itemKey.Id);
            uQuery.AddCondition("item_type_id", itemKey.TypeId);
            uQuery.AddField("subscriptions", new QueryOperation("subscriptions", QueryOperations.Addition, by));

            core.Db.Query(uQuery);
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidIteminfoException : Exception
    {
    }
}
