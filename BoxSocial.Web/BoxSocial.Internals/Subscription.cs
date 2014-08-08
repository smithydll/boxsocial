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
using System.ComponentModel;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BoxSocial.IO;

namespace BoxSocial.Internals
{
    [DataTable("subscriptions")]
    public class Subscription : Item
    {
        [DataField("subscription_item", DataFieldKeys.Index, "i_subscription")]
        private ItemKey itemKey;
        [DataField("user_id", DataFieldKeys.Index, "i_subscription")]
        private long ownerId;
        [DataField("subscriber_type_id")] // FUTURE
        private long subscriberTypeId;
        [DataField("subscription_time_ut")]
        private long timeRaw;
        [DataField("subscription_ip", 55)]
        private string ip;

        private User owner;

        public ItemKey ItemKey
        {
            get
            {
                return itemKey;
            }
        }

        public ItemKey SubscriberKey
        {
            get
            {
                return new ItemKey(ownerId, BoxSocial.Internals.ItemType.GetTypeId(core, typeof(User)));
            }
        }

        public long ItemId
        {
            get
            {
                return itemKey.Id;
            }
        }

        public long UserId
        {
            get
            {
                return ownerId;
            }
        }

        public long TimeRaw
        {
            get
            {
                return timeRaw;
            }
        }

        public DateTime GetTime(UnixTime tz)
        {
            return tz.DateTimeFromMysql(timeRaw);
        }

        private Subscription(Core core, DataRow subscriptionRow)
            : base(core)
        {
            ItemLoad += new ItemLoadHandler(Subscription_ItemLoad); 

            //
            // Because this class does not have an ID, it should only
            // be able to construct itself from raw data.
            //

            loadItemInfo(subscriptionRow);
        }

        void Subscription_ItemLoad()
        {

        }

        public static bool IsSubscribed(Core core, ItemKey itemKey)
        {
            bool subscribed = false;

            if (core.Session.SignedIn)
            {
                SelectQuery query = Subscription.GetSelectQueryStub(core, typeof(Subscription));
                query.AddCondition("subscription_item_id", itemKey.Id);
                query.AddCondition("subscription_item_type_id", itemKey.TypeId);
                query.AddCondition("user_id", core.LoggedInMemberId);

                System.Data.Common.DbDataReader subscriptionReader = core.Db.ReaderQuery(query);

                subscribed = subscriptionReader.HasRows;

                subscriptionReader.Close();
                subscriptionReader.Dispose();
            }

            return subscribed;
        }

        public static bool SubscribeToItem(Core core, ItemKey itemKey)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            core.Db.BeginTransaction();

            SelectQuery query = Subscription.GetSelectQueryStub(core, typeof(Subscription));
            query.AddCondition("subscription_item_id", itemKey.Id);
            query.AddCondition("subscription_item_type_id", itemKey.TypeId);
            query.AddCondition("user_id", core.LoggedInMemberId);

            DataTable subscriptionDataTable = core.Db.Query(query);

            if (subscriptionDataTable.Rows.Count == 0)
            {
                InsertQuery iQuery = new InsertQuery(typeof(Subscription));
                iQuery.AddField("subscription_item_id", itemKey.Id);
                iQuery.AddField("subscription_item_type_id", itemKey.TypeId);
                iQuery.AddField("user_id", core.LoggedInMemberId);
                iQuery.AddField("subscription_time_ut", UnixTime.UnixTimeStamp());
                iQuery.AddField("subscription_ip", core.Session.IPAddress.ToString());

                core.Db.Query(iQuery);

                ItemInfo info = new ItemInfo(core, itemKey);
                info.IncrementSubscribers();

                UpdateQuery uQuery = new UpdateQuery(typeof(UserInfo));
                uQuery.AddField("user_subscriptions", new QueryOperation("user_subscriptions", QueryOperations.Addition, 1));
                uQuery.AddCondition("user_id", core.LoggedInMemberId);
                core.Db.Query(uQuery);

                return true;
            }
            else
            {
                throw new AlreadySubscribedException();
            }
        }

        public static bool UnsubscribeFromItem(Core core, ItemKey itemKey)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            core.Db.BeginTransaction();

            SelectQuery query = Subscription.GetSelectQueryStub(core, typeof(Subscription));
            query.AddCondition("subscription_item_id", itemKey.Id);
            query.AddCondition("subscription_item_type_id", itemKey.TypeId);
            query.AddCondition("user_id", core.LoggedInMemberId);

            DataTable subscriptionDataTable = core.Db.Query(query);

            if (subscriptionDataTable.Rows.Count == 1)
            {
                DeleteQuery dQuery = new DeleteQuery(typeof(Subscription));
                dQuery.AddCondition("subscription_item_id", itemKey.Id);
                dQuery.AddCondition("subscription_item_type_id", itemKey.TypeId);
                dQuery.AddCondition("user_id", core.LoggedInMemberId);

                core.Db.Query(dQuery);

                ItemInfo info = new ItemInfo(core, itemKey);
                info.DecrementSubscribers();

                UpdateQuery uQuery = new UpdateQuery(typeof(UserInfo));
                uQuery.AddField("user_subscriptions", new QueryOperation("user_subscriptions", QueryOperations.Subtraction, 1));
                uQuery.AddCondition("user_id", core.LoggedInMemberId);
                core.Db.Query(uQuery);

                return true;
            }

            return false;
        }

        public static List<User> GetUserSubscribers(Core core, ItemKey itemKey, int page, int perPage)
        {
            List<User> subscribers = new List<User>();

            SelectQuery query = Subscription.GetSelectQueryStub(core, typeof(Subscription));
            query.AddCondition("subscription_item_id", itemKey.Id);
            query.AddCondition("subscription_item_type_id", itemKey.TypeId);
            query.AddFields(User.GetFieldsPrefixed(core, typeof(User)));
            query.AddFields(UserInfo.GetFieldsPrefixed(core, typeof(UserInfo)));
            query.AddFields(UserProfile.GetFieldsPrefixed(core, typeof(UserProfile)));
            query.AddField(new DataField("gallery_items", "gallery_item_uri"));
            query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
            query.AddJoin(JoinTypes.Inner, User.GetTable(typeof(User)), "user_id", "user_id");
            query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "user_id", "user_id");
            query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "user_id", "user_id");
            query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
            query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
            query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
            query.AddSort(SortOrder.Ascending, "subscription_time_ut");
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;

            DataTable subscribersDataTable = core.Db.Query(query);

            foreach (DataRow dr in subscribersDataTable.Rows)
            {
                subscribers.Add(new User(core, dr, UserLoadOptions.All));
            }

            return subscribers;
        }

        public static List<ItemKey> GetSubscribers(Core core, ItemKey itemKey, int page, int perPage)
        {
            List<ItemKey> subscribers = new List<ItemKey>();

            SelectQuery query = Subscription.GetSelectQueryStub(core, typeof(Subscription));
            query.AddCondition("subscription_item_id", itemKey.Id);
            query.AddCondition("subscription_item_type_id", itemKey.TypeId);
            query.AddSort(SortOrder.Ascending, "subscription_time_ut");
            if (page > 0 && perPage > 0)
            {
                query.LimitStart = (page - 1) * perPage;
                query.LimitCount = perPage;
            }

            DataTable subscribersDataTable = core.Db.Query(query);

            foreach (DataRow dr in subscribersDataTable.Rows)
            {
                Subscription subscription = new Subscription(core, dr);

                subscribers.Add(subscription.SubscriberKey);
            }

            return subscribers;
        }

        public static List<User> GetSubscriptions(Core core, User owner, int page, int perPage)
        {
            List<User> subscribers = new List<User>();

            SelectQuery query = Subscription.GetSelectQueryStub(core, typeof(Subscription));
            query.AddCondition(new DataField(typeof(Subscription), "user_id"), owner.Id);
            query.AddCondition("subscription_item_type_id", ItemKey.GetTypeId(core, typeof(User)));
            query.AddFields(User.GetFieldsPrefixed(core, typeof(User)));
            query.AddFields(UserInfo.GetFieldsPrefixed(core, typeof(UserInfo)));
            query.AddFields(UserProfile.GetFieldsPrefixed(core, typeof(UserProfile)));
            query.AddField(new DataField("gallery_items", "gallery_item_uri"));
            query.AddField(new DataField("gallery_items", "gallery_item_parent_path"));
            query.AddJoin(JoinTypes.Inner, User.GetTable(typeof(User)), "subscription_item_id", "user_id");
            query.AddJoin(JoinTypes.Inner, UserInfo.GetTable(typeof(UserInfo)), "subscription_item_id", "user_id");
            query.AddJoin(JoinTypes.Inner, UserProfile.GetTable(typeof(UserProfile)), "subscription_item_id", "user_id");
            query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_country"), new DataField("countries", "country_iso"));
            query.AddJoin(JoinTypes.Left, new DataField("user_profile", "profile_religion"), new DataField("religions", "religion_id"));
            query.AddJoin(JoinTypes.Left, new DataField("user_info", "user_icon"), new DataField("gallery_items", "gallery_item_id"));
            query.AddSort(SortOrder.Ascending, "subscription_time_ut");
            query.LimitStart = (page - 1) * perPage;
            query.LimitCount = perPage;

            DataTable subscribersDataTable = core.Db.Query(query);

            foreach (DataRow dr in subscribersDataTable.Rows)
            {
                subscribers.Add(new User(core, dr, UserLoadOptions.All));
            }

            return subscribers;
        }

        public override string Uri
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }

    public class ItemSubscribedEventArgs : EventArgs
    {
        private ItemKey itemKey;
        private User rater;

        public ItemKey ItemKey
        {
            get
            {
                return itemKey;
            }
        }

        public long ItemId
        {
            get
            {
                return itemKey.Id;
            }
        }

        public User Rater
        {
            get
            {
                return rater;
            }
        }

        public ItemSubscribedEventArgs(User rater, ItemKey itemKey)
        {
            this.rater = rater;
            this.itemKey = itemKey;
        }
    }

    public class ItemUnsubscribedEventArgs : EventArgs
    {
        private ItemKey itemKey;
        private User rater;

        public ItemKey ItemKey
        {
            get
            {
                return itemKey;
            }
        }

        public long ItemId
        {
            get
            {
                return itemKey.Id;
            }
        }

        public User Rater
        {
            get
            {
                return rater;
            }
        }

        public ItemUnsubscribedEventArgs(User rater, ItemKey itemKey)
        {
            this.rater = rater;
            this.itemKey = itemKey;
        }
    }

    public class AlreadySubscribedException : Exception
    {
    }

    public class InvalidSubscriptionException : Exception
    {
    }
}
