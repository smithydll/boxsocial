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
    [DataTable("notification_subscriptions")]
    public class NotificationSubscription : NumberedItem
    {
        [DataField("notification_subscription_id", DataFieldKeys.Primary)]
        private long subscriptionId;
        [DataField("notification_item")]
        private ItemKey itemKey;
        [DataField("notification_subscriber")]
        private ItemKey subscriberKey;
        [DataField("notification_subscribed")]
        private bool subscribed;
        [DataField("notification_subscribed_time_ut")]
        private long subscribedTime;

        public ItemKey SubscriberKey
        {
            get
            {
                return subscriberKey;
            }
        }

        public ItemKey ItemKey
        {
            get
            {
                return itemKey;
            }
        }

        public bool IsSubscribed
        {
            get
            {
                return subscribed;
            }
            set
            {
                SetPropertyByRef(new { subscribed }, value);
            }
        }

        public NotificationSubscription(Core core, DataRow notificationSubscriptionRow)
            : base(core)
        {
            try
            {
                loadItemInfo(notificationSubscriptionRow);
            }
            catch (InvalidItemException)
            {
                throw new InvalidNotificationSubscriptionException();
            }
        }

        public static NotificationSubscription Create(Core core, User receiver, ItemKey itemKey)
        {
            if (core == null)
            {
                throw new NullCoreException();
            }

            return (NotificationSubscription)Item.Create(core, typeof(NotificationSubscription),
                new FieldValuePair("notification_subscriber_id", receiver.Id),
                new FieldValuePair("notification_subscriber_type_id", ItemKey.GetTypeId(typeof(User))),
                new FieldValuePair("notification_item_id", itemKey.Id),
                new FieldValuePair("notification_item_type_id", itemKey.TypeId),
                new FieldValuePair("notification_subscribed", true),
                new FieldValuePair("notification_subscribed_time_ut", UnixTime.UnixTimeStamp()));
        }

        public static void Unsubscribe(Core core, User receiver, ItemKey itemKey)
        {
            UpdateQuery query = new UpdateQuery(typeof(NotificationSubscription));
            query.AddCondition("notification_subscriber_id", receiver.Id);
            query.AddCondition("notification_subscriber_type_id", receiver.TypeId);
            query.AddCondition("notification_item_id", itemKey.Id);
            query.AddCondition("notification_item_type_id", itemKey.TypeId);
            query.AddField("notification_subscribed", true);

            core.Db.Query(query);
        }

        public static List<ItemKey> GetNotificationSubscribers(Core core, ItemKey itemKey)
        {
            List<ItemKey> subscribers = new List<ItemKey>();

            SelectQuery query = NotificationSubscription.GetSelectQueryStub(typeof(NotificationSubscription));
            query.AddCondition("notification_item_id", itemKey.Id);
            query.AddCondition("notification_item_type_id", itemKey.TypeId);
            query.AddCondition("notification_subscribed", true);

            DataTable subscribersDataTable = core.Db.Query(query);

            foreach (DataRow notificationSubscriptionRow in subscribersDataTable.Rows)
            {
                NotificationSubscription subscription = new NotificationSubscription(core, notificationSubscriptionRow);

                subscribers.Add(subscription.SubscriberKey);
            }

            return subscribers;
        }


        public override long Id
        {
            get
            {
                return subscriptionId;
            }
        }

        public override string Uri
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class InvalidNotificationSubscriptionException : Exception
    {
    }
}
