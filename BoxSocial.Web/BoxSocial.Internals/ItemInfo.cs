﻿/*
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
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("item_info")]
    [JsonObject("item_info")]
    public class ItemInfo : Item
    {
        [DataField("info_item", DataFieldKeys.Unique)]
        private ItemKey itemKey;
        [DataField("info_shortkey", DataFieldKeys.Unique, 11)]
        private string shortUrlKey;
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
        [DataField("info_subscribers")]
        private long subscribers;
        [DataField("info_tags")]
        private long tags;
        [DataField("info_shared_times")]
        private long sharedTimes;
        [DataField("info_viewed_times")]
        private long viewedTimes;
        [DataField("info_tweet_id")]
        private long tweetId;
        [DataField("info_tweet_uri", 31)]
        private string tweetUri;
        [DataField("info_facebook_post_id", 63)]
        private string facebookPostId;
        [DataField("info_tumblr_post_id")]
        private long tumblrPostId;
        [DataField("info_item_time_ut")]
        private long timeRaw;

        private NumberedItem item;

        [JsonIgnore]
        public ItemKey InfoKey
        {
            get
            {
                return itemKey;
            }
        }

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

            SelectQuery query = ItemInfo.GetSelectQueryStub(core, typeof(ItemInfo));
            query.AddCondition("info_item_id", itemId);
            query.AddCondition("info_item_type_id", itemTypeId);

            System.Data.Common.DbDataReader infoReader = db.ReaderQuery(query);

            try
            {
                if (infoReader.HasRows)
                {
                    infoReader.Read();

                    loadItemInfo(infoReader);

                    infoReader.Close();
                    infoReader.Dispose();
                }
                else
                {
                    infoReader.Close();
                    infoReader.Dispose();

                    throw new InvalidIteminfoException();
                }
            }
            catch (InvalidItemException)
            {
                throw new InvalidIteminfoException();
            }
        }

        public ItemInfo(Core core, string key)
            : base(core)
        {
            this.item = item;
            ItemLoad += new ItemLoadHandler(ItemInfo_ItemLoad);

            SelectQuery query = ItemInfo.GetSelectQueryStub(core, typeof(ItemInfo));
            query.AddCondition("info_shortkey", key);

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

        public ItemInfo(Core core, System.Data.Common.DbDataReader itemReader)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemInfo_ItemLoad);

            try
            {
                loadItemInfo(itemReader);
            }
            catch (InvalidItemException)
            {
                throw new InvalidIteminfoException();
            }
        }

        public ItemInfo(Core core, HibernateItem itemReader)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(ItemInfo_ItemLoad);

            try
            {
                loadItemInfo(itemReader);
            }
            catch (InvalidItemException)
            {
                throw new InvalidIteminfoException();
            }
        }

        protected override void loadItemInfo(DataRow itemRow)
        {
            loadValue(itemRow, "info_item", out itemKey);
            loadValue(itemRow, "info_shortkey", out shortUrlKey);
            comments = (long)itemRow["info_comments"];
            likes = (long)itemRow["info_likes"];
            dislikes = (long)itemRow["info_dislikes"];
            rating = (float)itemRow["info_rating"];
            subscribers = (long)itemRow["info_subscribers"];
            tags = (long)itemRow["info_tags"];
            sharedTimes = (long)itemRow["info_shared_times"];
            viewedTimes = (long)itemRow["info_viewed_times"];
            tweetId = (long)itemRow["info_tweet_id"];
            loadValue(itemRow, "info_tweet_uri", out tweetUri);
            loadValue(itemRow, "info_facebook_post_id", out facebookPostId);
            tumblrPostId = (long)itemRow["info_tumblr_post_id"];
            timeRaw = (long)itemRow["info_item_time_ut"];

            itemLoaded(itemRow);
        }

        protected override void loadItemInfo(System.Data.Common.DbDataReader itemRow)
        {
            loadValue(itemRow, "info_item", out itemKey);
            loadValue(itemRow, "info_shortkey", out shortUrlKey);
            comments = (long)itemRow["info_comments"];
            likes = (long)itemRow["info_likes"];
            dislikes = (long)itemRow["info_dislikes"];
            rating = (float)itemRow["info_rating"];
            subscribers = (long)itemRow["info_subscribers"];
            tags = (long)itemRow["info_tags"];
            sharedTimes = (long)itemRow["info_shared_times"];
            viewedTimes = (long)itemRow["info_viewed_times"];
            tweetId = (long)itemRow["info_tweet_id"];
            loadValue(itemRow, "info_tweet_uri", out tweetUri);
            loadValue(itemRow, "info_facebook_post_id", out facebookPostId);
            tumblrPostId = (long)itemRow["info_tumblr_post_id"];
            timeRaw = (long)itemRow["info_item_time_ut"];

            itemLoaded(itemRow);
        }

        protected override void loadItemInfo(HibernateItem itemRow)
        {
            loadValue(itemRow, "info_item", out itemKey);
            loadValue(itemRow, "info_shortkey", out shortUrlKey);
            comments = (long)itemRow["info_comments"];
            likes = (long)itemRow["info_likes"];
            dislikes = (long)itemRow["info_dislikes"];
            rating = (float)itemRow["info_rating"];
            subscribers = (long)itemRow["info_subscribers"];
            tags = (long)itemRow["info_tags"];
            sharedTimes = (long)itemRow["info_shared_times"];
            viewedTimes = (long)itemRow["info_viewed_times"];
            tweetId = (long)itemRow["info_tweet_id"];
            loadValue(itemRow, "info_tweet_uri", out tweetUri);
            loadValue(itemRow, "info_facebook_post_id", out facebookPostId);
            tumblrPostId = (long)itemRow["info_tumblr_post_id"];
            timeRaw = (long)itemRow["info_item_time_ut"];

            itemLoaded(itemRow);
        }

        protected override bool setProperty(string field, object value)
        {
            switch (field)
            {
                case "info_item":
                    itemKey = (ItemKey)value;
                    return true;
                case "info_shortkey":
                    shortUrlKey = (string)value;
                    return true;
                case "info_comments":
                    comments = (long)value;
                    return true;
                case "info_likes":
                    likes = (long)value;
                    return true;
                case "info_dislikes":
                    dislikes = (long)value;
                    return true;
                case "info_ratings":
                    ratings = (long)value;
                    return true;
                case "info_rating":
                    rating = (float)value;
                    return true;
                case "info_subscribers":
                    subscribers = (long)value;
                    return true;
                case "info_tags":
                    tags = (long)value;
                    return true;
                case "info_shared_times":
                    sharedTimes = (long)value;
                    return true;
                case "info_viewed_times":
                    viewedTimes = (long)value;
                    return true;
                case "info_tweet_id":
                    tweetId = (long)value;
                    return true;
                case "info_tweet_uri":
                    tweetUri = (string)value;
                    return true;
                case "info_facebook_post_id":
                    facebookPostId = (string)value;
                    return true;
                case "info_tumblr_post_id":
                    tumblrPostId = (long)value;
                    return true;
                case "info_item_time_ut":
                    timeRaw = (long)value;
                    return true;
            }

            return false;
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

            byte[] encryptBytes = { 0x44, 0x33, 0x22, 0x11, 0x00, 0x99, 0x88, 0x77 };
            string encryptKey = "boxsocia";

            try
            {
                byte[] bytes = new byte[] { 
                (byte)(itemKey.TypeId & 0x00FF), 
                (byte)(((itemKey.TypeId & 0xFF00) >> 8) + ((itemKey.Id & 0xFF0000000000) >> 40)), 
                (byte)((itemKey.Id & 0x00FF00000000) >> 32), 
                (byte)((itemKey.Id & 0x0000FF000000) >> 24), 
                (byte)((itemKey.Id & 0x000000FF0000) >> 16), 
                (byte)((itemKey.Id & 0x00000000FF00) >> 8), 
                (byte)((itemKey.Id & 0x0000000000FF)) };

                byte[] keyBytes = Encoding.UTF8.GetBytes(encryptKey);
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(keyBytes, encryptBytes), CryptoStreamMode.Write);
                cs.Write(bytes, 0, bytes.Length);
                cs.FlushFinalBlock();

                bytes = ms.ToArray();

                string shortKey = Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Trim(new char[] { '=' });

                // We need to make this atomic with the rest of the page
                core.Db.BeginTransaction();

                InsertQuery iQuery = new InsertQuery(typeof(ItemInfo));
                iQuery.AddField("info_item_id", itemKey.Id);
                iQuery.AddField("info_item_type_id", itemKey.TypeId);
                iQuery.AddField("info_shortkey", shortKey);
                iQuery.AddField("info_item_time_ut", UnixTime.UnixTimeStamp());

                core.Db.Query(iQuery);
                core.Db.CommitTransaction();

                ItemInfo ii = new ItemInfo(core, item, itemKey.Id, itemKey.TypeId);

                return ii;
            }
            catch (OverflowException)
            {
                HttpContext.Current.Response.Write("OVERFLOW: type: " + itemKey.TypeId.ToString() + ", id: " + itemKey.Id.ToString() + "<br />");
                HttpContext.Current.Response.End();
                return null;
            }
        }

        [JsonProperty("comments")]
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

        [JsonProperty("likes")]
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

        [JsonProperty("dislikes")]
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

        [JsonProperty("ratings")]
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

        [JsonProperty("rating")]
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

        [JsonProperty("subscribers")]
        public long Subscribers
        {
            get
            {
                return subscribers;
            }
            internal set
            {
                SetPropertyByRef(new { subscriptions = subscribers }, value);
            }
        }

        [JsonProperty("shares")]
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

        [JsonProperty("views")]
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

        internal void IncrementSubscribers()
        {
            AdjustSubscribers(1);
        }

        internal void DecrementSubscribers()
        {
            AdjustSubscribers(-1);
        }

        internal void AdjustSubscribers(int by)
        {
            UpdateQuery uQuery = new UpdateQuery(typeof(ItemInfo));
            uQuery.AddCondition("info_item_id", itemKey.Id);
            uQuery.AddCondition("info_item_type_id", itemKey.TypeId);
            uQuery.AddField("info_subscribers", new QueryOperation("info_subscribers", QueryOperations.Addition, by));

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

        [JsonIgnore]
        internal long TweetId
        {
            get
            {
                return tweetId;
            }
            set
            {
                tweetId = value;
            }
        }

        [JsonIgnore]
        internal string FacebookPostId
        {
            get
            {
                return facebookPostId;
            }
            set
            {
                facebookPostId = value;
            }
        }

        [JsonIgnore]
        internal long TumblrPostId
        {
            get
            {
                return tumblrPostId;
            }
            set
            {
                tumblrPostId = value;
            }
        }

        [JsonIgnore]
        private ItemKey ItemKey
        {
            get
            {
                return itemKey;
            }
        }

        [JsonIgnore]
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

        [JsonIgnore]
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

        [JsonProperty("share_uri")]
        public string ShareUri
        {
            get
            {
                return core.Hyperlink.StripSid(core.Hyperlink.AppendAbsoluteSid("/s/" + shortUrlKey));
            }
        }

        [JsonIgnore]
        public string UniqueString
        {
            get
            {
                return shortUrlKey;
            }
        }
    }

    public class InvalidIteminfoException : Exception
    {
    }
}
